namespace LeaseEtcdManager;

public enum LeaseState
{
    Unknown,
    Leader,
    Follower,
    Lost
}