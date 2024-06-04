Naming Conventions
---

If there is a difference between a British-English and an American-English word, the British-English word is used (e.g. colour and not color)

Naming of variables follows a (mostly) normal C# naming convention:
- Public fields: PascalCase (e.g. MyPublicField)
- Private fields: camelCase with '_' prefix (e.g. _myPrivateField)
- Local variables: camelCase (e.g. myLocalVariable)

Non-conventional naming:
- Matrices: Always start with 'm_' (e.g. m_model). If a matrix is public, it should follow PascalCase and should use the full name of the matrix (eg ProjectionMatrix)

Variables declared in GLSL (or other language files) follow the same naming conventions.

GLSL Specific Naming:
- Vertex attributes start with 'v' and the attribute (e.g. vPosition, vNormal)

Formatting
---

Formatting follows a normal C# formatting system:
- 4 space indent
- When initialising an array, use collection initialisers and not array initialisers. Leave a space before the first array element, and after the last array element as it makes large arrays slightly more readable. When using multiline collection initialisers, leave a trailing comma
```csharp
int[] myArr = { 0, 1, 2 }; // Don't use this
int[] myArr = [ 0, 1, 2 ]; // Use this
```
```csharp
string[] names = 
[
    "First Name",
    "Second Name",
    "Third Name", // <--- Trailing comma
];
```
- Curly brackets go on their own line, unless writing a function as part of a function parameter

```csharp
void NewFunction() // Newline curly brackets
{
    // Code
}
```
```csharp
SomeFunction(() => { // Same line opening bracket
    // Code
});
```
