//Signed distances field

float sdBox(float3 p, float3 s)
{
    float3 q = abs(p) - s;
    return length(max(q, 0.0f)) + min(max(q.x, max(q.y, q.z)), 0.0f);
}

float sdSphere(float3 p, float3 s)
{
    float2 k = float2(length(p / s), length(p / (s * s)));
    return k.x * (k.x - 1.0f) / k.y;
}

float sdCylinder(float3 p, float3 s)
{
    float2 d = abs(float2(length(p.xz), p.y)) - s.xy;
    return min(max(d.x, d.y), 0.0f) + length(max(d, 0.0f));
}

float sdCone(float3 p, float3 s)
{
    p.y -= s.y;
    s.y *= 2.0f;
    float2 q = s.y * float2(0.5f * (s.x + s.z) / s.y, -1.0f);
    float2 w = float2(length(p.xz), p.y);
    
    float2 a = w - q * clamp(dot(w, q) / dot(q, q), 0.0f, 1.0f);
    float2 b = w - q * float2(clamp(w.x / q.x, 0.0f, 1.0f), 1.0f);
    float k = sign(q.y);
    float d = min(dot(a, a), dot(b, b));
    float u = max(k * (w.x * q.y - w.y * q.x), k * (w.y - q.y));
    return sqrt(d) * sign(u);
}

float sdSolidAngle(float3 p, float3 s)
{
    p.y += s.y;
    s.y *= 3.0f;
    
    float ra = sqrt(2.0f) * length(s.xz);
    float a = atan(ra / s.y);
    float2 c = float2(sin(a), cos(a));
    
    float2 q = float2(length(p.xz), p.y);
    float l = length(q) - ra;
    float m = length(q - c * clamp(dot(q, c), 0.0, ra));
    return max(l, m * sign(c.y * q.x - c.x * q.y));
}

float sdTorus(float3 p, float3 s)
{
    float2 q = float2(length(p.xz) - s.x, p.y);
    return length(q) - s.y;
}

float sdTriPrism(float3 p, float2 h)
{
    float3 q = abs(p);
    return max(q.z - h.y, max(q.x * 0.866025 + p.y * 0.5, -p.y) - h.x * 0.5);
}

float sdHexPrism(float3 p, float2 h)
{
    const float3 k = float3(-0.8660254, 0.5, 0.57735);
    p = abs(p);
    p.xy -= 2.0 * min(dot(k.xy, p.xy), 0.0) * k.xy;
    float2 d = float2(length(p.xy - float2(clamp(p.x, -k.z * h.x, k.z * h.x), h.x)) * sign(p.y - h.x), p.z - h.y);
    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0));
}

float sdPyramid(float3 p, float h)
{
    float m2 = h * h + 0.25;
    
    p.xz = abs(p.xz);
    p.xz = (p.z > p.x) ? p.zx : p.xz;
    p.xz -= 0.5;

    float3 q = float3(p.z, h * p.y - 0.5 * p.x, h * p.x + 0.5 * p.y);
   
    float s = max(-q.x, 0.0);
    float t = clamp((q.y - 0.5 * p.z) / (m2 + 0.25), 0.0, 1.0);
    
    float a = m2 * (q.x + s) * (q.x + s) + q.y * q.y;
    float b = m2 * (q.x + 0.5 * t) * (q.x + 0.5 * t) + (q.y - m2 * t) * (q.y - m2 * t);
    
    float d2 = min(q.y, -q.x * m2 - q.y * 0.5) > 0.0 ? 0.0 : min(a, b);
    
    return sqrt((d2 + q.z * q.z) / m2) * sign(max(q.z, -p.y));
}

float sdOctahedron(float3 p, float s)
{
    p = abs(p);
    float m = p.x + p.y + p.z - s;
    
    float3 q;
    if (3.0 * p.x < m)
        q = p.xyz;
    else if (3.0 * p.y < m)
        q = p.yzx;
    else if (3.0 * p.z < m)
        q = p.zxy;
    else
        return m * 0.57735027;
    
    float k = clamp(0.5 * (q.z - q.y + s), 0.0, s);
    return length(float3(q.x, q.y - s + k, q.z - k));
}

//Operators

float opRound(float d, float k)
{
    return d - k;
}

float opOnion(float d, float k)
{
    return abs(d) - k;
}

float3 opBend(float3 p, float k)
{
    float c = cos(k * p.x);
    float s = sin(k * p.x);
    float2x2 m = float2x2(c, -s, s, c);
    float3 q = float3(mul(m, p.xy), p.z);
    return q;
}

float3 opTwist(float3 p, float k)
{
    float c = cos(k * p.y);
    float s = sin(k * p.y);
    float2x2 m = float2x2(c, -s, s, c);
    float3 q = float3(mul(m, p.xz), p.y);
    return q;
}

float opUnion(float a, float b)
{
    return min(a, b);
}

float opInter(float a, float b)
{
    return max(a, b);
}

float opSub(float a, float b)
{
    return max(a, -b);
}

float opXor(float a, float b)
{
    return opSub(opUnion(a, b), opInter(a, b));
}

float4 opUnion(float4 a, float4 b)
{
    return a.w < b.w ? a : b;
}

float4 opInter(float4 a, float4 b)
{
    return a.w > b.w ? a : b;
}

float4 opSub(float4 a, float4 b)
{
    return a.w > -b.w ? a : float4(b.xyz, -b.w);
}

float4 opXor(float4 a, float4 b)
{
    return opSub(opUnion(a, b), opInter(a, b));
}

float opSUnion(float a, float b, float k, out float h)
{
    h = clamp(0.5f + 0.5f * (b - a) / k, 0.0f, 1.0f);
    return lerp(b, a, h) - k * h * (1.0f - h);
}

float opSInter(float a, float b, float k, out float h)
{
    h = clamp(0.5f - 0.5f * (b - a) / k, 0.0f, 1.0f);
    return lerp(b, a, h) + k * h * (1.0f - h);
}

float opSSub(float a, float b, float k, out float h)
{
    h = clamp(0.5f - 0.5f * (b + a) / k, 0.0f, 1.0f);
    return lerp(a, -b, h) + k * h * (1.0f - h);
}

float opSXor(float a, float b, float k, out float h)
{
    float hu, hi;
    return opSSub(opSUnion(a, b, k, hu), opSInter(a, b, k, hi), k, h);
}

float4 opSUnion(float4 a, float4 b, float k)
{
    float h;
    float d = opSUnion(a.w, b.w, k, h);
    return float4(lerp(b.xyz, a.xyz, h), d);
}

float4 opSInter(float4 a, float4 b, float k)
{
    float h;
    float d = opSInter(a.w, b.w, k, h);
    return float4(lerp(b.xyz, a.xyz, h), d);
}

float4 opSSub(float4 a, float4 b, float k)
{
    float h;
    float d = opSSub(a.w, b.w, k, h);
    return float4(lerp(a.xyz, b.xyz, h), d);
}

float4 opSXor(float4 a, float4 b, float k)
{
    return opSSub(opSUnion(a, b, k), opSInter(a, b, k), k);
}

// Main Functions

float sdf(uint type, float4x4 wtlm, float4 size, float4 model, float3 pos, float dmax)
{
    float df = dmax;
    float round = model.x, onion = model.y, bend = model.z, twist = model.w;
    
    pos = mul(wtlm, float4(pos, 1.0f)).xyz;
    
    const float eps = 10e-3f;
    
    if (type > 0 && type < 11)
    {
        if (bend > eps)
        {
            pos = opBend(pos, bend);
        }
    
        if (twist > eps)
        {
            pos = opTwist(pos, twist);
        }
    
        if (type == 1)
        {
            df = sdBox(pos, size.xyz);
        }
        else if (type == 2)
        {
            df = sdSphere(pos, size.xyz);
        }
        else if (type == 3)
        {
            df = sdCylinder(pos, size.xyz);
        }
        else if (type == 4)
        {
            df = sdCone(pos, size.xyz);
        }
        else if (type == 5)
        {
            df = sdSolidAngle(pos, size.xyz);
        }
        else if (type == 6)
        {
            df = sdTorus(pos, size.xyz);
        }
        else if (type == 7)
        {
            df = sdTriPrism(pos, float2(length(size.xz), size.y));
        }
        else if (type == 8)
        {
            df = sdHexPrism(pos, float2((size.x + size.z) / 2, size.y));
        }
        else if (type == 9)
        {
            df = sdPyramid(pos, length(size.xyz));
        }
        else if (type == 10)
        {
            df = sdOctahedron(pos, (size.x + size.y + size.z) / 3);
        }
        
        if (round > eps)
        {
            df = opRound(df, round);
        }
    
        if (onion > eps)
        {
            df = opOnion(df, round);
        }
    }
    return df;
}

float compose(uint type, float smooth, float df1, float df2)
{
    float df = df1;
    float h;
    
    const float eps = 10e-3f;
    
    if (type == 0)
    {
        df = smooth < eps ? opUnion(df1, df2) : opSUnion(df1, df2, smooth, h);
    }
    else if (type == 1)
    {
        df = smooth < eps ? opInter(df1, df2) : opSInter(df1, df2, smooth, h);
    }
    else if (type == 2)
    {
        df = smooth < eps ? opSub(df1, df2) : opSSub(df1, df2, smooth, h);
    }
    else if (type == 3)
    {
        df = smooth < eps ? opXor(df1, df2) : opSXor(df1, df2, smooth, h);
    }
    
    return df;
}

float4 compose(uint type, float smooth, float4 df1, float4 df2)
{
    float4 df = df1;
    
    const float eps = 10e-3f;
    
    if (type == 0)
    {
        df = smooth < eps ? opUnion(df1, df2) : opSUnion(df1, df2, smooth);
    }
    else if (type == 1)
    {
        df = smooth < eps ? opInter(df1, df2) : opSInter(df1, df2, smooth);
    }
    else if (type == 2)
    {
        df = smooth < eps ? opSub(df1, df2) : opSSub(df1, df2, smooth);
    }
    else if (type == 3)
    {
        df = smooth < eps ? opXor(df1, df2) : opSXor(df1, df2, smooth);
    }
    
    return df;
}