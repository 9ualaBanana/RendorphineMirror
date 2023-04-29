using System.Data.Common;
using System.Data.SQLite;

namespace NodeCommon;

public class Database
{
    public static Database Instance => LazyInstance.Value;
    static readonly Lazy<Database> LazyInstance = new(() => new Database());

    public readonly string DbPath;
    readonly SQLiteConnection Connection;

    public Database(string? dbpath = null)
    {
        DbPath = dbpath ?? Path.Combine(Directories.Data, "config.db");
        Connection = new SQLiteConnection("Data Source=" + DbPath + ";Version=3;cache=shared");
        if (!File.Exists(DbPath)) SQLiteConnection.CreateFile(DbPath);

        Connection.Open();
        OperationResult.WrapException(() => ExecuteNonQuery("PRAGMA cache=shared;")).LogIfError();
    }

    public int ExecuteNonQuery(string command)
    {
        using var cmd = new SQLiteCommand(command, Connection);
        return cmd.ExecuteNonQuery();
    }
    public int ExecuteNonQuery(string command, params DbParameter[] parameters) => ExecuteNonQuery(command, parameters.AsEnumerable());
    public int ExecuteNonQuery(string command, IEnumerable<DbParameter> parameters)
    {
        using var cmd = new SQLiteCommand(command, Connection);

        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);

        return cmd.ExecuteNonQuery();
    }

    public DbDataReader ExecuteQuery(string command)
    {
        using var cmd = new SQLiteCommand(command, Connection);
        return cmd.ExecuteReader();
    }
    public DbDataReader ExecuteQuery(string command, params DbParameter[] parameters) => ExecuteQuery(command, parameters.AsEnumerable());
    public DbDataReader ExecuteQuery(string command, IEnumerable<DbParameter> parameters)
    {
        using var cmd = new SQLiteCommand(command, Connection);

        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);

        return cmd.ExecuteReader();
    }

    public DbTransaction BeginTransaction() => Connection.BeginTransaction();



    readonly List<WeakReference<IDatabaseBindable>> TrackedBindables = new();

    public void Track(IDatabaseBindable bindable) => TrackedBindables.Add(new(bindable));
    public void ReloadAllBindables()
    {
        foreach (var weakref in TrackedBindables)
            if (weakref.TryGetTarget(out var bindable))
                bindable.Reload();
    }
}
