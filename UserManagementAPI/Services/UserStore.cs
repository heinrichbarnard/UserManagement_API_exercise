using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

public sealed class UserStore
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, User> _users = [];

    public IReadOnlyList<User> GetAll()
    {
        lock (_gate)
        {
            return _users.Values.ToArray();
        }
    }

    public User? Get(Guid id)
    {
        lock (_gate)
        {
            return _users.GetValueOrDefault(id);
        }
    }

    public bool TryCreate(string name, string email, out User user)
    {
        lock (_gate)
        {
            if (_users.Values.Any(existing =>
                    string.Equals(existing.Email, email, StringComparison.OrdinalIgnoreCase)))
            {
                user = null!;
                return false;
            }

            var now = DateTimeOffset.UtcNow;
            user = new User(Guid.NewGuid(), name.Trim(), email.Trim(), now, now);
            _users.Add(user.Id, user);
            return true;
        }
    }

    public UserUpdateResult TryUpdate(Guid id, string name, string email, out User? updatedUser)
    {
        lock (_gate)
        {
            if (!_users.ContainsKey(id))
            {
                updatedUser = null;
                return UserUpdateResult.NotFound;
            }

            if (_users.Values.Any(existing =>
                    existing.Id != id &&
                    string.Equals(existing.Email, email, StringComparison.OrdinalIgnoreCase)))
            {
                updatedUser = null;
                return UserUpdateResult.EmailConflict;
            }

            var existingUser = _users[id];
            updatedUser = existingUser with
            {
                Name = name.Trim(),
                Email = email.Trim(),
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };
            _users[id] = updatedUser;
            return UserUpdateResult.Updated;
        }
    }

    public bool TryDelete(Guid id)
    {
        lock (_gate)
        {
            return _users.Remove(id);
        }
    }
}

public enum UserUpdateResult
{
    Updated,
    NotFound,
    EmailConflict
}
