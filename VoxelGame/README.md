Naming Conventions
---

## General
- If there is a difference between a British-English and an American-English word, use the British-English word (e.g. colour, not color).

## C# Naming Conventions
- **Public fields**: PascalCase (e.g. `MyPublicField`)
- **Private fields**: camelCase with `_` prefix (e.g. `_myPrivateField`)
- **Local variables**: camelCase (e.g. `myLocalVariable`)

## Non-conventional Naming
- **Matrices**: Always start with `m_` (e.g. `m_model`).
    - Public matrices: Follow PascalCase and use the full name of the matrix (e.g. `ProjectionMatrix`).

## GLSL Naming Conventions

Formatting
---

Formatting follows a normal C# formatting system:
- 4 space indent
- When initialising an array, use collection initialisers and not array initialisers. Leave a space before the first array element, and after the last array element as it makes large arrays slightly more readable. When using multiline collection initialisers, leave a trailing comma. Also ensure that all commas are aligned with the longest array element
```csharp
int[] myArr = { 0, 1, 2 }; // Don't use this
int[] myArr = [ 0, 1, 2 ]; // Use this
```
```csharp
string[] names = 
[
    "First Name" , // <--- comma has whitespace before to be aligned with element 2
    "Second Name",
    "Third Name" , // <--- Trailing comma
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
<br>
GLSL functions use same-line opening curly brackets. GLSL functions are named using standard camel case. Declare function prototypes at the top of the file but implement them under the main function<br>

```glsl
void someFuntion(); // function prototype

void main() {
    // Main code
}

void someFuntion() {
    // Code
}
```
