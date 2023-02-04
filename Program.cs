using SimdIteration;

Random random = new(Environment.TickCount);
int[] items = new int[10];
items.Mutate((index, value) => random.Next(index, 1000) + value);
Console.WriteLine(items.Length);