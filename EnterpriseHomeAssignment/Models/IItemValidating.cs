namespace EnterpriseHomeAssignment.Models
{
    public interface IItemValidating
    {
        List<string> GetValidatorEmails();
        string GetCardPartial();
    }
}
