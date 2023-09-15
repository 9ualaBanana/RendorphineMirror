namespace Node.Tasks;

public class CondaEnvironmentWasNotCreatedException : Exception
{
    public CondaEnvironmentWasNotCreatedException(string env) : base($"Conda environment {env} was not created") { }
}
