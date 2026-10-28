using System;

namespace G3D
{
    public class GMaterial
    {
        public string name;
        public Color3 diffuseColor = Color3.white();
        public Color3 specularColor = Color3.black();
        public Color3 emissiveColor = Color3.black();
        public float shininess = 0;
        public ReferenceCountedPointer<Texture> diffuseTexture;
        public ReferenceCountedPointer<Texture> normalMap;
        public ReferenceCountedPointer<Texture> specularMap;

        public GMaterial()
        {
        }

        public GMaterial(string name)
        {
            this.name = name;
        }
    }
}