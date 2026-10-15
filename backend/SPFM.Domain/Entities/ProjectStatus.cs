namespace SPFM.Domain.Entities;

/// <summary>
/// Project lifecycle states.
///
/// OPEN QUESTION: requirements s28 says to implement the states the client's
/// specification defines and warns against inventing status logic. These are
/// placeholders pending that list.
/// </summary>
public enum ProjectStatus
{
    Quote = 0,
    Active = 1,
    Complete = 2,
    Archived = 3,
}
