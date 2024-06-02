Naming Conventions
---

Naming of variables follows a (mostly) normal C# naming convention:
- Public fields: PascalCase (e.g. MyPublicField)
- Private fields: camelCase with _ prefix (e.g. _myPrivateField)
- Local variables: camelCase (e.g. myLocalVariable)

Non-conventional naming:
- Matrices: Always start with m_ (e.g. m_modelMatrix). The only exception is the Player class where m_proj is replaced by ProjectionMatrix and m_view is replaced by ViewMatrix.

Variables declared in GLSL (or other language files) follow the same naming conventions.