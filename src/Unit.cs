/// <summary>
/// Represents a void type since you can't use void as a generic parameter.
/// </summary>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>
{
    public static readonly Unit Value = new();
    public static readonly ValueTask<Unit> ValueTask = new(Value);
    
    public bool Equals(Unit other) => true;
    public override bool Equals(object? obj) => obj is Unit;
    public override int GetHashCode() => 0;
    public int CompareTo(Unit other) => 0;
    public override string ToString() => "()";
    
    public static bool operator ==(Unit left, Unit right) => true;
    public static bool operator !=(Unit left, Unit right) => false;
}
