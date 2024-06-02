Naming Conventions
---

If there is a difference between a British-English and an American-English word, the British-English word is used.

Naming of variables follows a (mostly) normal C# naming convention:
- Public fields: PascalCase (e.g. MyPublicField)
- Private fields: camelCase with '_' prefix (e.g. _myPrivateField)
- Local variables: camelCase (e.g. myLocalVariable)

Non-conventional naming:
- Matrices: Always start with 'm_' (e.g. m_model). If a matrix is public, it should follow PascalCase and should use the full name of the matrix (eg ProjectionMatrix)

Variables declared in GLSL (or other language files) follow the same naming conventions.

GLSL Specific Naming:
- Vertex attributes start with 'v' and the attribute (e.g. vPosition, vNormal)