#ifdef GL_ES
precision mediump float;
#endif


// --------================= LIBRARIES =================--------

// ============ Random ============

// Description : Array and textureless GLSL 2D simplex noise function.
//      Author : Ian McEwan, Ashima Arts.
//  Maintainer : stegu
//     Lastmod : 20110822 (ijm)
//     License : Copyright (C) 2011 Ashima Arts. All rights reserved.
//               Distributed under the MIT License. See LICENSE file.
//               https://github.com/ashima/webgl-noise
//               https://github.com/stegu/webgl-noise

vec3 mod289(vec3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
vec2 mod289(vec2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
vec3 permute(vec3 x) { return mod289(((x*34.0)+10.0)*x); }

float snoise(vec2 v) {
    const vec4 C = vec4(0.211324865405187,  // (3.0-sqrt(3.0))/6.0
                      0.366025403784439,  // 0.5*(sqrt(3.0)-1.0)
                     -0.577350269189626,  // -1.0 + 2.0 * C.x
                      0.024390243902439); // 1.0 / 41.0
	// First corner
    vec2 i  = floor(v + dot(v, C.yy) );
    vec2 x0 = v -   i + dot(i, C.xx);

	// Other corners
    vec2 i1;
    //i1.x = step( x0.y, x0.x ); // x0.x > x0.y ? 1.0 : 0.0
    //i1.y = 1.0 - i1.x;
    i1 = (x0.x > x0.y) ? vec2(1.0, 0.0) : vec2(0.0, 1.0);
    // x0 = x0 - 0.0 + 0.0 * C.xx ;
    // x1 = x0 - i1 + 1.0 * C.xx ;
    // x2 = x0 - 1.0 + 2.0 * C.xx ;
    vec4 x12 = x0.xyxy + C.xxzz;
    x12.xy -= i1;

	// Permutations
  	i = mod289(i); // Avoid truncation effects in permutation
    vec3 p = permute( permute( i.y + vec3(0.0, i1.y, 1.0 ))
        + i.x + vec3(0.0, i1.x, 1.0 ));

    vec3 m = max(0.5 - vec3(dot(x0,x0), dot(x12.xy,x12.xy), dot(x12.zw,x12.zw)), 0.0);
    m = m*m ;
    m = m*m ;

    // Gradients: 41 points uniformly over a line, mapped onto a diamond.
    // The ring size 17*17 = 289 is close to a multiple of 41 (41*7 = 287)

    vec3 x = 2.0 * fract(p * C.www) - 1.0;
    vec3 h = abs(x) - 0.5;
    vec3 ox = floor(x + 0.5);
    vec3 a0 = x - ox;

    // Normalise gradients implicitly by scaling m
    // Approximation of: m *= inversesqrt( a0*a0 + h*h );
  	m *= 1.79284291400159 - 0.85373472095314 * ( a0*a0 + h*h );

	// Compute final noise value at P
    vec3 g;
    g.x  = a0.x  * x0.x  + h.x  * x0.y;
    g.yz = a0.yz * x12.xz + h.yz * x12.yw;
    return 130.0 * dot(m, g);
}

float snoise1(vec2 p) {
    return (snoise(p) + 1.0) / 2.0;
}

// -------------------------------------------------------------


// ============ Variables ============
uniform vec2 u_resolution;
uniform vec2 u_mouse;
uniform float u_time;

const float PIXEL_SIZE = 0.01;
const vec4 BACKGROUND_COLOR = vec4(0.052,0.051,0.065,1.000);
const vec4 GROUND_COLOR = vec4(0.435,0.360,0.291,1.000);
const vec4 GRASS_COLOR = vec4(0.373,0.605,0.318,1.000);
const vec4 EMPTY_COLOR = vec4(0,0,0,0);
const int GRASS_MIN = 1;
const int GRASS_MAX = 2;

vec2 planetPosition = vec2(0, 0);
float planetSize = 0.0;


// ============ Data ============
const int MAX_DATA = 10;
vec4 data(int v) { float pct = float(v) / float(MAX_DATA); return vec4(pct, pct, pct, 1); }
int data(vec4 d) { return int(d.x * float(MAX_DATA)); }


// ============ Utility ============

vec2 pixellateXY(vec2 p, float PIXEL_SIZE, vec2 pixelOffset) {
    return floor((p - pixelOffset) / PIXEL_SIZE) * PIXEL_SIZE + pixelOffset;
}

vec4 drawCircle(vec2 p, vec4 col, vec2 cc, float r, vec4 ccol) {
    vec2 dir = p - cc;
    float dstSq = dir.x * dir.x + dir.y * dir.y;
    if (dstSq < r * r) col = ccol;
    return col;
}

int abs(int a) { if (a < 0) return -a; else return a; }

float length(int dx, int dy) { return sqrt(float(dx * dx + dy * dy)); }
float lengthSq(int dx, int dy) { return float(dx * dx + dy * dy); }

vec4 layer(vec4 a, vec4 b) {
    if (b.w != 0.0) return b;
    return a;
}


// ============ Samples ============

vec4 sampleMesh_DataRaw(vec2 p) {
    vec4 col = data(-1);
    
    // Draw circle
    vec2 cc = planetPosition;
    float cr = planetSize;
    col = drawCircle(p, col, cc, cr, data(1));
    col = drawCircle(p, col, cc + vec2(0.2,0.0), cr * 0.6, data(1));
    
    return col;
}

vec4 sampleMesh_Data(vec2 p) {
    // Pixellate position
    vec2 pp = pixellateXY(p, PIXEL_SIZE, planetPosition);
    vec4 col = sampleMesh_DataRaw(pp);
    
    return col;
}

vec4 sampleMesh_Final(vec2 p) {
    vec4 col = sampleMesh_Data(p);
    
    // Color ground / background
    if (data(col) == -1) col = BACKGROUND_COLOR;
    else if (data(col) == 1) col = GROUND_COLOR;
    
    return col;
}


vec4 sampleOutline_Data(vec2 p) {
    vec2 pp = pixellateXY(p, PIXEL_SIZE, planetPosition);
    vec4 scol = sampleMesh_Data(p);
    
    // Generate border around outside
    vec4 col = data(-1);
    if (data(scol) == -1) {
        vec4 su = sampleMesh_Data(p + vec2( 0,  1) * PIXEL_SIZE);
        vec4 sr = sampleMesh_Data(p + vec2( 1,  0) * PIXEL_SIZE);
        vec4 sd = sampleMesh_Data(p + vec2( 0, -1) * PIXEL_SIZE);
        vec4 sl = sampleMesh_Data(p + vec2(-1,  0) * PIXEL_SIZE);
        bool isBorder = (data(su) != -1 || data(sr) != -1 || data(sd) != -1 || data(sl) != -1);
        
        // Assign data
        if (isBorder) {
            float n = snoise1(pp * 10.0);
            int d = GRASS_MIN + int(n * float(GRASS_MAX - GRASS_MIN + 1));
            col = data(d);
        }
    }
    
    return col;
}

vec4 sampleOutline_Final(vec2 p) {
    // Only outline if ground
    vec4 col = EMPTY_COLOR;
    if (data(sampleOutline_Data(p)) == 0) return GRASS_COLOR;
    // if (data(sampleMesh_Data(p)) == -1) return col;
    
    // Setup variables
    int closestDst = -1;
    int closestData = -1;
    bool isGrass = false;

	// Loop over kernel
    for (int x = -GRASS_MAX; x <= GRASS_MAX; x++) {
        if (isGrass) continue;
        for (int y = -GRASS_MAX; y <= GRASS_MAX; y++) {
            if (isGrass) continue;

            // If potentially closer
            int dst = int(length(x, y));
            if (closestDst == -1 || dst < closestDst) {

                // Check if closer than data
                int d = data(sampleOutline_Data(p + vec2(x, y) * PIXEL_SIZE));
                if (d != -1 && dst <= d) {
                    closestDst = dst;
                    closestData = d;
                    isGrass = true;
                }
            }
        }
    }

    // Is grass so recolor
    if (isGrass) col = GRASS_COLOR;
    
    return col;
}


vec4 sample_Final(vec2 p) {
    vec4 mesh = sampleMesh_Final(p);
    vec4 outline = sampleOutline_Final(p);
    vec4 col = mesh;
    col = layer(col, outline);
	return col;
}


// ============ Main ============ 

void main() {
    vec2 st = gl_FragCoord.xy/u_resolution.xy;
    st.x *= u_resolution.x/u_resolution.y;
    
    planetPosition = vec2(0.5+sin(u_time * 0.28) * 0.15, 0.5+cos(u_time * 0.11) * 0.07);
    planetSize = 0.25 + 0.02 * sin(u_time * 0.7);
    
	vec4 col = sample_Final(st.xy);
    
    gl_FragColor = col;
}
