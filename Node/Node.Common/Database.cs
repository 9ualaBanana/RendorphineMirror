using System.Data.SQLite;

namespace Node.Common;

public class Database : IDisposable
{
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public readonly string DbPath;
    readonly SQLiteConnection Connection;

    public Database(string dbpath)
    {
        DbPath = dbpath;
        Connection = new SQLiteConnection("Data Source=" + DbPath + ";Version=3;cache=shared");
        if (!File.Exists(DbPath)) SQLiteConnection.CreateFile(DbPath);

        Connection.Open();
        OperationResult.WrapException(() => ExecuteNonQuery("PRAGMA cache=shared;")).LogIfError();

        new Thread(() =>
        {
            while (true)
            {
                Logger.Info($"[Database {dbpath}] Optimizing");
                OperationResult.WrapException(() => ExecuteNonQuery("PRAGMA optimize;")).LogIfError();
                OperationResult.WrapException(() => ExecuteNonQuery("vacuum;")).LogIfError();

                Thread.Sleep(60 * 60 * 24 * 1000);
            }
        })
        { IsBackground = true }.Start();
    }

    public int ExecuteNonQuery(string command)
    {
        using var cmd = new SQLiteCommand(command, Connection);
        return cmd.ExecuteNonQuery();
    }
    public int ExecuteNonQuery(string command, params SQLiteParameter[] parameters) => ExecuteNonQuery(command, parameters.AsEnumerable());
    public int ExecuteNonQuery(string command, IEnumerable<SQLiteParameter> parameters)
    {
        using var cmd = new SQLiteCommand(command, Connection);

        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);

        return cmd.ExecuteNonQuery();
    }

    public SQLiteDataReader ExecuteQuery(string command)
    {
        using var cmd = new SQLiteCommand(command, Connection);
        return cmd.ExecuteReader();
    }
    public SQLiteDataReader ExecuteQuery(string command, params SQLiteParameter[] parameters) => ExecuteQuery(command, parameters.AsEnumerable());
    public SQLiteDataReader ExecuteQuery(string command, IEnumerable<SQLiteParameter> parameters)
    {
        using var cmd = new SQLiteCommand(command, Connection);

        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);

        return cmd.ExecuteReader();
    }

    public SQLiteTransaction BeginTransaction() => Connection.BeginTransaction();


    public void Dispose()
    {
        Connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
