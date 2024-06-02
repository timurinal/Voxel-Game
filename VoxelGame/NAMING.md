Naming Conventions
---

Naming of variables follows a (mostly) normal C# naming convention:
- Public fields: PascalCase (e.g. MyPublicField)
- Private fields: camelCase with _ prefix (e.g. _myPrivateField)
- Local variables: camelCase (e.g. myLocalVariable)

Non-conventional naming:
- Matrices: Always start with m_ (e.g. m_modelMatrix). If a matrix is public, it should follow PascalCase and should use the full name of the matrix (eg ProjectionMatrix)

Variables declared in GLSL (or other language files) follow the same naming conventions.