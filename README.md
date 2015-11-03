# Oven.ReadOnly
bake a ReadOnlyInterface

Magical Usage
----
```c#
public interface IReadOnlyFoo
{
    int a { get; }
    int b { get; }

    void Print(int a,int b);
}
public class Foo
{
    public int a { get; set; }
    public int b { get; set; }

    public void Print(int a,int b)
    {
        Console.WriteLine("Hello" + (a+b).ToString());
    }
    public void NonConstMethod()
    {
        /* asdfsdf */
    }
}
```
```c#
Foo f = new Foo();

f.a = 123;

/* bake a `IReadOnlyFoo` */
var bread = ReadOnlyOven.Bake<IReadOnlyFoo, Foo>(f);

Console.WriteLine(bread.a);
bread.Print(5,5 );
```
