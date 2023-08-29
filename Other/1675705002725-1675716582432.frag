

#ifdef GL_ES
precision mediump float;
#endif


// ============ Variables ============

uniform vec2 u_resolution;
uniform vec2 u_mouse;
uniform float u_time;

const float PIXEL_SIZE = 0.01;
const vec4 BACKGROUND_COLOR = vec4(0.052,0.051,0.065,1.000);
const vec4 GROUND_COLOR = vec4(0.435,0.360,0.291,1.000);
const vec4 GRASS_COLOR = vec4(0.373,0.605,0.318,1.000);
const int MIN_BORDER = 2;
const int MAX_BORDER = 20;
const vec4 WHITE = vec4(1.0, 1.0, 1.0, 1.0);
const vec4 BLACK = vec4(0.0, 0.0, 0.0, 1.0);

vec2 planetPosition = vec2(0, 0);
float planetSize = 0.0;


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


// ============ Samples ============

vec4 sample0(vec2 p) {
    vec4 col = BLACK;
    
    // Draw circle
    vec2 cc = planetPosition;
    float cr = planetSize;
    col = drawCircle(p, col, cc, cr, WHITE);
    
    return col;
}

vec4 sample1(vec2 p) {
    vec4 col = sample0(p);
    
    // Color ground / background
    if (col == BLACK) col = BACKGROUND_COLOR;
    else if (col == WHITE) col = GROUND_COLOR;
    
    return col;
}

vec4 sample2(vec2 p) {
    // Pixellate position
    vec2 pp = pixellateXY(p, PIXEL_SIZE, planetPosition);
    vec4 col = sample1(pp);
    
    return col;
}

vec4 sample3(vec2 p) {
    vec4 col = sample2(p);
    
    // Generate border around outside
    if (col == BACKGROUND_COLOR) {
        vec4 su = sample2(p + vec2( 0,  1) * PIXEL_SIZE);
        vec4 sr = sample2(p + vec2( 1,  0) * PIXEL_SIZE);
        vec4 sd = sample2(p + vec2( 0, -1) * PIXEL_SIZE);
        vec4 sl = sample2(p + vec2(-1,  0) * PIXEL_SIZE);
        bool isBorder = (su != BACKGROUND_COLOR || sr != BACKGROUND_COLOR || sd != BACKGROUND_COLOR || sl != BACKGROUND_COLOR);
        if (isBorder) col = GRASS_COLOR;
    }
    
    return col;
}

vec4 sample4(vec2 p) {
    vec4 col = sample3(p);
    
    
    
    // Find distance to closest border
    // Noise based on closest border position
    
    
    // Ground so find closest empty
//     if (col != BACKGROUND_COLOR) {
//     	bool isGrass = false;
        
//     	// Check in a square
//         int borderWidth = MIN_BORDER + int(snoise(p * 0.410) * float(MAX_BORDER - MIN_BORDER));
//         // int borderWidth = MIN_BORDER;
        
//         for (int x = -MAX_BORDER; x <= MAX_BORDER; x++) {
//         	if (isGrass || x < -borderWidth || x > borderWidth) continue;
            
//             for (int y = -MAX_BORDER; y <= MAX_BORDER; y++) {
//         		if (isGrass || y < -borderWidth || y > borderWidth) continue;
                
//                 vec4 sc = sample2(p + vec2(-3, -3) * PIXEL_SIZE);
                
//     			if (sc == BACKGROUND_COLOR) isGrass = true;
//             }
//         }
        
//         // Is grass so recolor
//     	if (isGrass) col = GRASS_COLOR;
//     }
    
    return col;
}


// ============ Main ============ 

void main() {
    vec2 st = gl_FragCoord.xy/u_resolution.xy;
    st.x *= u_resolution.x/u_resolution.y;
    
    planetPosition = vec2(0.5+sin(u_time * 0.28) * 0.15, 0.5+cos(u_time * 0.11) * 0.07);
    planetSize = 0.25 + 0.02 * sin(u_time * 0.7);
    
	vec4 col = sample3(st.xy);
    
    gl_FragColor = col;
}
