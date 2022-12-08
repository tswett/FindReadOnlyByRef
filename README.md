This is a code analyzer for finding a certain problem in VB.NET code.

In VB.NET, if a ReadOnly property is accidentally passed into a ByRef parameter of a function, the compiler accepts this with no warning, and the value that's assigned is silently discarded!

Here's an example:

``` lang-vb
Structure Point
    Public ReadOnly Property X As Integer
    Public ReadOnly Property Y As Integer
End Structure

Module Module1
    Sub IncreaseByOne(ByRef x As Integer)
        x = x + 1
    End Sub

    Sub Main()
        Dim point As New Point
        IncreaseByOne(point.X)
        Console.WriteLine($"point.X is {point.X}")
    End Sub
End Module
```

You would probably hope that the line `IncreaseByOne(point.X)` would throw an error, or at least a warning, since `point.X` is read-only and it doesn't make sense to pass it by reference. Instead, the code compiles with no warnings, and the value assigned to `x` inside of `IncreaseByOne` is silently discarded, and the program prints `point.X is 0`.

This code analyzer will detect all of the places in your code where a read-only field or property is passed into a function that takes it by reference, and will issue a warning for each one.

See https://stackoverflow.com/questions/74709423/how-can-i-detect-when-a-readonly-property-has-accidentally-been-passed-by-refere
