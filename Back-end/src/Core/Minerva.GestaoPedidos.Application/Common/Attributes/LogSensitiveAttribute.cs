namespace Minerva.GestaoPedidos.Application.Common.Attributes;

/// <summary>
/// Attribute to mark properties that contain sensitive data (PII) that should be masked in logs.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class LogSensitiveAttribute : Attribute
{
}