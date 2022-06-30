using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace DigitalOpus.MB.Core
{
    /// <summary>
    /// A TextureBlender will attempt to blend non-texture properties with textures so that the result material looks the same as source material.
    /// </summary>
    public interface TextureBlender
    {    
        /// <summary>
        /// The shader name that must be matched on the result material in order for this TextureBlender to be used. This should return something like "Legacy/Bumped Difuse"
        /// </summary>
        bool DoesShaderNameMatch(string shaderName);

        /// <summary>
        /// This is called to prepare the TextureBlender before any calls to OnBlendTexturePixel
        /// Use this to grab the non-texture property values from the material that will be used to alter the Pixel color in the texture.
        /// Note that the sourceMat may not use a shader matching ShaderName. It may not have expected properties. Check that properties exist
        /// before grabing them.
        /// </summary>
        void OnBeforeTintTexture(Material sourceMat, string shaderTexturePropertyName);

        /// <summary>
        /// Called once for each pixel in the texture to alter the pixel color. For efficiency don't check shaderPropertyName every call. Instead use OnBeforeTintTexture
        /// to prepare this textrure blender for a batch of OnBlendTexturePixel calls.
        /// </summary>
        Color OnBlendTexturePixel(string shaderPropertyName, Color pixelColor);

        /// <summary>
        /// Material a & b may have the same set of textures but different non-texture properties (colorTint etc...)
        /// If so then they need to be put into separate rectangels in the atlas. This method should check the non-texture properties 
        /// and return false if they are different. Note that material a and b may use a different shader than GetShaderName so your code
        /// should handle the case where properties do not exist.
        /// </summary>
        bool NonTexturePropertiesAreEqual(Material a, Material b);

        /// <summary>
        /// Sets the non texture properties on the result materail after textures have been baked. If for example _Color has been blended with 
        /// the _Albedo textures then the _Color property on the result material should probably be set to white.
        /// </summary>
        void SetNonTexturePropertyValuesOnResultMaterial(Material resultMaterial);

        /// <summary>
        /// Some textures may not be assigned for a material. This method should return a color that will used to create a small solid color texture
        /// to be used in these cases. Note that this small solid color texture will later be blended using OnBlendTexturePixel. If the texturePropertyname is _mainTex
        /// then the the returned color should probably be white so it looks correct when OnBlendTexturePixel blends the _Color.
        /// 
        /// This is also used to determine if an atlas needs to be generated for a texture property. If all the source materials are missing the texture for
        /// texPropertyName property (eg. _MainTex), but some of the source materials return different value for:
        ///     OnBlendTexturePixel(texturePropertyName, GetColorIfNoTexture(sourceMat, texturePropertyName))
        /// Then an atlas will be generated with the different colors.
        /// 
        /// This method can also be used to collect the value of non texture properties and cache them for each source material. This information can be useful
        /// for setting values on the result material.
        /// </summary>
        Color GetColorIfNoTexture(Material m, ShaderTextureProperty texPropertyName);
    }
}
