namespace VoxelGame.Graphics.Shaders.Compiler;

public enum Token
{
    NONE,
    
    SHADER,
    PASS,
    INCLUDE,
    PRAGMA,
    STRUCT,
    UNIFORM,
    
    FLOAT,
    FLOAT2,
    FLOAT3,
    FLOAT4,
    
    INT,
    INT2,
    INT3,
    INT4,
    
    HALF,
    HALF2,
    HALF3,
    HALF4,
    
    DOUBLE,
    DOUBLE2,
    DOUBLE3,
    DOUBLE4,
    
    BOOLEAN,
    
    SAMPLER2D,
    
    IDENTIFIER,
    NUMBER,
    STRING,
    COMMENT,
    NEWLINE,
    PREPROCESSORDIRECTIVE,
    
    LBRACE,
    RBRACE,
    LBRACKET,
    RBRACKET,
    SEMICOLON,
    COLON,
    COMMA
}

public static class TokenExtensions
{
    public static Token StringToToken(this Token token, string t)
    {
        return t switch
        {
            "Shader"  => Token.SHADER,
            "Pass"    => Token.PASS,
            "include" => Token.INCLUDE,
            "pragma"  => Token.PRAGMA,
            "struct"  => Token.STRUCT,
            "uniform" => Token.UNIFORM,
            
            "float"   => Token.FLOAT,
            "float2"  => Token.FLOAT2,
            "float3"  => Token.FLOAT3,
            "float4"  => Token.FLOAT4,
            
            "int"     => Token.INT,
            "int2"    => Token.INT2,
            "int3"    => Token.INT3,
            "int4"    => Token.INT4,
            
            "half"    => Token.HALF,
            "half2"   => Token.HALF2,
            "half3"   => Token.HALF3,
            "half4"   => Token.HALF4,
            
            "double"  => Token.DOUBLE,
            "double2" => Token.DOUBLE2,
            "double3" => Token.DOUBLE3,
            "double4" => Token.DOUBLE4,
            
            "bool"      => Token.BOOLEAN,
            "sampler2D" => Token.SAMPLER2D,
            
            @"[a-zA-Z_][a-zA-Z_0-9]*" => Token.IDENTIFIER,
            @"[0-9]+" => Token.NUMBER,
            @"'""' .*? '""'" => Token.STRING,
            @"\n" => Token.NEWLINE,
            @"\r\n" => Token.NEWLINE,
            "#" => Token.PREPROCESSORDIRECTIVE,
            
            _ => Token.NONE
        };
    }
}