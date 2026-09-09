namespace ViteKlub.Core.Data;

public static class DemoDatasetValidator
{
    public static IReadOnlyList<DatasetValidationError> Validate(DemoDataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        var errors = new List<DatasetValidationError>();

        if (dataset.SchemaVersion != DemoDataset.CurrentSchemaVersion)
        {
            errors.Add(new(
                "dataset.schema.unsupported",
                "schemaVersion",
                $"La versione schema {dataset.SchemaVersion} non è supportata."));
        }

        ValidateRequiredDatasetMetadata(dataset, errors);
        ValidateEntityMetadata(dataset, errors);
        ValidateUniqueIds(dataset, errors);
        ValidateUsers(dataset.Users, errors);
        ValidateMembers(dataset.Members, errors);
        ValidatePlans(dataset.MembershipPlans, errors);
        ValidateSubscriptions(dataset, errors);
        ValidateAccesses(dataset, errors);
        ValidatePayments(dataset, errors);
        ValidateAuditEvents(dataset, errors);

        return errors.AsReadOnly();
    }

    private static void ValidateRequiredDatasetMetadata(
        DemoDataset dataset,
        List<DatasetValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(dataset.DatasetVersion))
        {
            errors.Add(new("dataset.version.required", "datasetVersion", "La versione del dataset è obbligatoria."));
        }

        if (dataset.ReferenceDate == default)
        {
            errors.Add(new("dataset.referenceDate.required", "referenceDate", "La data di riferimento è obbligatoria."));
        }
    }

    private static void ValidateEntityMetadata(
        DemoDataset dataset,
        List<DatasetValidationError> errors)
    {
        foreach ((string path, DemoEntity entity) in EnumerateEntities(dataset))
        {
            if (entity.Id == Guid.Empty)
            {
                errors.Add(new("entity.id.required", $"{path}.id", "L'identificativo non può essere vuoto."));
            }

            if (entity.Version < 1)
            {
                errors.Add(new("entity.version.invalid", $"{path}.version", "La versione deve essere positiva."));
            }

            if (entity.CreatedAtUtc == default || entity.UpdatedAtUtc < entity.CreatedAtUtc)
            {
                errors.Add(new("entity.timestamps.invalid", path, "I timestamp dell'entità non sono coerenti."));
            }
        }
    }

    private static void ValidateUniqueIds(
        DemoDataset dataset,
        List<DatasetValidationError> errors)
    {
        foreach (IGrouping<Guid, (string Path, DemoEntity Entity)> duplicate in EnumerateEntities(dataset)
                     .GroupBy(item => item.Entity.Id)
                     .Where(group => group.Key != Guid.Empty && group.Count() > 1))
        {
            errors.Add(new(
                "entity.id.duplicate",
                duplicate.First().Path,
                $"L'identificativo {duplicate.Key} è utilizzato da più entità."));
        }
    }

    private static void ValidateUsers(
        IReadOnlyList<DemoUser> users,
        List<DatasetValidationError> errors)
    {
        AddDuplicateStringErrors(users.Select((user, index) => (user.Username, $"users[{index}].username")), errors);

        if (!users.Any(user => user.IsActive && user.Role == DemoRole.Administrator))
        {
            errors.Add(new("users.administrator.required", "users", "È richiesto almeno un Administrator attivo."));
        }

        for (var index = 0; index < users.Count; index++)
        {
            DemoUser user = users[index];
            if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.DisplayName))
            {
                errors.Add(new("user.name.required", $"users[{index}]", "Username e nome visualizzato sono obbligatori."));
            }
        }
    }

    private static void ValidateMembers(
        IReadOnlyList<Member> members,
        List<DatasetValidationError> errors)
    {
        AddDuplicateStringErrors(
            members.Select((member, index) => (member.MemberNumber, $"members[{index}].memberNumber")),
            errors);

        for (var index = 0; index < members.Count; index++)
        {
            Member member = members[index];
            if (string.IsNullOrWhiteSpace(member.MemberNumber)
                || string.IsNullOrWhiteSpace(member.FirstName)
                || string.IsNullOrWhiteSpace(member.LastName))
            {
                errors.Add(new("member.name.required", $"members[{index}]", "Numero tessera, nome e cognome sono obbligatori."));
            }

            if (member.DateOfBirth >= member.JoinedOn)
            {
                errors.Add(new("member.dates.invalid", $"members[{index}]", "La data di nascita deve precedere l'iscrizione."));
            }
        }
    }

    private static void ValidatePlans(
        IReadOnlyList<MembershipPlan> plans,
        List<DatasetValidationError> errors)
    {
        for (var index = 0; index < plans.Count; index++)
        {
            MembershipPlan plan = plans[index];
            if (string.IsNullOrWhiteSpace(plan.Name) || plan.DurationDays < 1 || plan.Price < 0)
            {
                errors.Add(new("plan.values.invalid", $"membershipPlans[{index}]", "Nome, durata e prezzo del piano non sono validi."));
            }

            bool invalidEntries = plan.Type == MembershipPlanType.EntryBased
                ? plan.IncludedEntries is null or < 1
                : plan.IncludedEntries is not null;
            if (invalidEntries)
            {
                errors.Add(new("plan.entries.invalid", $"membershipPlans[{index}].includedEntries", "Gli ingressi non sono coerenti con il tipo di piano."));
            }

            ValidateCurrency(plan.Currency, $"membershipPlans[{index}].currency", errors);
        }
    }

    private static void ValidateSubscriptions(
        DemoDataset dataset,
        List<DatasetValidationError> errors)
    {
        var memberIds = dataset.Members.Select(member => member.Id).ToHashSet();
        var planIds = dataset.MembershipPlans.Select(plan => plan.Id).ToHashSet();

        for (var index = 0; index < dataset.Subscriptions.Count; index++)
        {
            MemberSubscription subscription = dataset.Subscriptions[index];
            string path = $"subscriptions[{index}]";
            AddMissingReference(subscription.MemberId, memberIds, $"{path}.memberId", errors);
            AddMissingReference(subscription.MembershipPlanId, planIds, $"{path}.membershipPlanId", errors);

            if (subscription.EndsOn < subscription.StartsOn || subscription.PurchasePrice < 0)
            {
                errors.Add(new("subscription.values.invalid", path, "Date o prezzo dell'abbonamento non sono validi."));
            }

            if (subscription.RemainingEntries is < 0)
            {
                errors.Add(new("subscription.entries.invalid", $"{path}.remainingEntries", "Gli ingressi residui non possono essere negativi."));
            }

            ValidateCurrency(subscription.Currency, $"{path}.currency", errors);
        }
    }

    private static void ValidateAccesses(
        DemoDataset dataset,
        List<DatasetValidationError> errors)
    {
        var memberIds = dataset.Members.Select(member => member.Id).ToHashSet();
        var subscriptionIds = dataset.Subscriptions.Select(subscription => subscription.Id).ToHashSet();
        var userIds = dataset.Users.Select(user => user.Id).ToHashSet();

        for (var index = 0; index < dataset.Accesses.Count; index++)
        {
            GymAccess access = dataset.Accesses[index];
            string path = $"accesses[{index}]";
            AddMissingReference(access.MemberId, memberIds, $"{path}.memberId", errors);
            AddMissingReference(access.RecordedByUserId, userIds, $"{path}.recordedByUserId", errors);
            AddOptionalMissingReference(access.SubscriptionId, subscriptionIds, $"{path}.subscriptionId", errors);

            if (access.Outcome == AccessOutcome.Granted && access.DenialReason != AccessDenialReason.None)
            {
                errors.Add(new("access.denialReason.invalid", $"{path}.denialReason", "Un accesso consentito non può avere una causa di rifiuto."));
            }

            if (access.Outcome == AccessOutcome.Denied && access.DenialReason == AccessDenialReason.None)
            {
                errors.Add(new("access.denialReason.required", $"{path}.denialReason", "Un accesso negato deve indicare una causa."));
            }
        }
    }

    private static void ValidatePayments(
        DemoDataset dataset,
        List<DatasetValidationError> errors)
    {
        var memberIds = dataset.Members.Select(member => member.Id).ToHashSet();
        var subscriptionIds = dataset.Subscriptions.Select(subscription => subscription.Id).ToHashSet();
        var userIds = dataset.Users.Select(user => user.Id).ToHashSet();

        for (var index = 0; index < dataset.Payments.Count; index++)
        {
            Payment payment = dataset.Payments[index];
            string path = $"payments[{index}]";
            AddMissingReference(payment.MemberId, memberIds, $"{path}.memberId", errors);
            AddMissingReference(payment.RecordedByUserId, userIds, $"{path}.recordedByUserId", errors);
            AddOptionalMissingReference(payment.SubscriptionId, subscriptionIds, $"{path}.subscriptionId", errors);

            if (payment.Amount < 0)
            {
                errors.Add(new("payment.amount.invalid", $"{path}.amount", "L'importo non può essere negativo."));
            }

            ValidateCurrency(payment.Currency, $"{path}.currency", errors);
        }
    }

    private static void ValidateAuditEvents(
        DemoDataset dataset,
        List<DatasetValidationError> errors)
    {
        var userIds = dataset.Users.Select(user => user.Id).ToHashSet();
        for (var index = 0; index < dataset.AuditEvents.Count; index++)
        {
            AuditEvent auditEvent = dataset.AuditEvents[index];
            AddMissingReference(auditEvent.ActorUserId, userIds, $"auditEvents[{index}].actorUserId", errors);
        }
    }

    private static IEnumerable<(string Path, DemoEntity Entity)> EnumerateEntities(DemoDataset dataset)
    {
        return Enumerate("users", dataset.Users)
            .Concat(Enumerate("members", dataset.Members))
            .Concat(Enumerate("membershipPlans", dataset.MembershipPlans))
            .Concat(Enumerate("subscriptions", dataset.Subscriptions))
            .Concat(Enumerate("accesses", dataset.Accesses))
            .Concat(Enumerate("payments", dataset.Payments))
            .Concat(Enumerate("auditEvents", dataset.AuditEvents));
    }

    private static IEnumerable<(string Path, DemoEntity Entity)> Enumerate<T>(
        string collectionName,
        IReadOnlyList<T> entities)
        where T : DemoEntity
    {
        return entities.Select((entity, index) => ($"{collectionName}[{index}]", (DemoEntity)entity));
    }

    private static void AddDuplicateStringErrors(
        IEnumerable<(string Value, string Path)> values,
        List<DatasetValidationError> errors)
    {
        foreach (IGrouping<string, (string Value, string Path)> duplicate in values
                     .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                     .GroupBy(item => item.Value, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            errors.Add(new("value.duplicate", duplicate.First().Path, $"Il valore '{duplicate.Key}' deve essere univoco."));
        }
    }

    private static void AddMissingReference(
        Guid id,
        HashSet<Guid> availableIds,
        string path,
        List<DatasetValidationError> errors)
    {
        if (!availableIds.Contains(id))
        {
            errors.Add(new("reference.missing", path, $"Il riferimento {id} non esiste."));
        }
    }

    private static void AddOptionalMissingReference(
        Guid? id,
        HashSet<Guid> availableIds,
        string path,
        List<DatasetValidationError> errors)
    {
        if (id is not null)
        {
            AddMissingReference(id.Value, availableIds, path, errors);
        }
    }

    private static void ValidateCurrency(
        string currency,
        string path,
        List<DatasetValidationError> errors)
    {
        if (!string.Equals(currency, "EUR", StringComparison.Ordinal))
        {
            errors.Add(new("currency.unsupported", path, "La demo supporta esclusivamente la valuta EUR."));
        }
    }
}
