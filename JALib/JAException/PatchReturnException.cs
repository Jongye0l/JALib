namespace JALib.JAException;

public class PatchReturnException(Type original, Type current) : Exception($"Patch return type mismatch: {original.FullName} -> {current.FullName}");