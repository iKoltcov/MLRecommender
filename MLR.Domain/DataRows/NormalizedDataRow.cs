namespace MLR.Domain.DataRows;

public class NormalizedDataRow
{
    /// <summary>
    /// Id of unique tuple (Role, Seniority, Specialization)
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Id of Pair (Skill, Proficiency)
    /// </summary>
    public int Skill { get; set; }
}