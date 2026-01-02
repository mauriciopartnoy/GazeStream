using System.Collections.Generic;
using System;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public class CommandRouter
{
    private readonly Dictionary<string, BaseWebsocketCommand> commands = new();

    public void RegisterCommand(BaseWebsocketCommand command)
    {
        if (commands.ContainsKey(command.Name))
        {
            Debug.WriteLine($"Command '{command.Name}' already registered. Overwriting.");
        }

        commands[command.Name] = command;
    }

    public void Execute(string json)
    {
        CommandMessage msg;
        try
        {
            msg = JsonConvert.DeserializeObject<CommandMessage>(json);
        }
        catch (Exception e)
        {
            return;
        }

        if (!commands.TryGetValue(msg.command, out var command))
        {
            Debug.WriteLine($"Unknown command '{msg.command}'.");
            return;
        }

        JObject parameters = msg.parameters ?? new JObject();

        if (!command.Validate(parameters, out var error))
        {
            Debug.WriteLine($"Validation failed for '{msg.command}': {error}");
            return;
        }

        command.Execute(parameters);
    }
}

[Serializable]
public class CommandMessage
{
    public string command;
    public JObject parameters;
}
public abstract class BaseWebsocketCommand
{
   public abstract string Name { get; }
    public abstract Dictionary<string, ParamSchema> Schema { get; }

    public bool Validate(JObject parameters, out string error)
    {
        foreach (var kvp in Schema)
        {
            string key = kvp.Key;
            Debug.WriteLine("Looking value for key: " + key);
            var expected = kvp.Value;

            if (!parameters.TryGetValue(key, out var token))
            {
                Debug.WriteLine("Parameter key was not found. Using default value.");
                // Apply default if optional
                if (expected.DefaultValue != null)
                {
                    parameters[key] = JToken.FromObject(expected.DefaultValue);
                    continue;
                }

                if (expected.Required)
                {
                    error = $"Missing required parameter '{key}' for command '{Name}'.";
                    return false;
                }

                continue;        
            }

            try 
            {
                token.ToObject(expected.Type);
            }
            catch
            {
                error = $"Parameter '{key}' has invalid type. Expected {expected.Type.Name}.";
                return false;
            }
        }

        error = null;
        return true;
    }

    public abstract void Execute(JObject parameters);


}

public class ParamSchema
{
    public Type Type { get; }
    public bool Required { get; }
    public object DefaultValue { get; }

    public ParamSchema(Type type, bool required = true, object defaultValue = null)
    {
        Type = type;
        Required = required;
        DefaultValue = defaultValue;
    }
}

