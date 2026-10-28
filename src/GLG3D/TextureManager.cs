/**
  @file TextureManager.h

  @maintainer Morgan McGuire, matrix@graphics3d.com
  @cite by Morgan McGuire & Peter Sibley

  @created 2003-11-25
  @edited  2006-02-01
*/

using System;
using G3D;

namespace G3D {

    /**
     Caches previously loaded textures.

     You can load textures manually using the G3D::Texture static methods.
     This class allows you to manage a set of textures with cached loading.
     Textures with no external references are cleared when the cache exceeds
     a pre-set size hint.

     @deprecated This will be moved to contrib in release 7.00
     */
    public class TextureManager {

        /**
         Wrapper for the arguments to a texture constructor
         */
        public class TextureArgs {
            public string filename;
            public TextureFormat format;
            public Texture.WrapMode wrap;
            public Texture.InterpolateMode interpolate;
            public Texture.Dimension dimension;
            public double brighten;

            public TextureArgs() {
                format = null;
            }

            public TextureArgs(TextureFormat _format) {
                format = _format;
            }

            public uint hashCode() {
                return
                    (uint)global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(format) +
                    (uint)wrap +
                    (uint)interpolate * 1027 +
                    (uint)dimension +
                    (uint)filename.GetHashCode() +
                    (uint)brighten;
            }

            public bool equals(TextureArgs other) {
                if (ReferenceEquals(other, null)) {
                    return false;
                }
                if (ReferenceEquals(this, other)) {
                    return true;
                }
                return
                    (format         == other.format)        &&
                    (wrap           == other.wrap)          &&
                    (interpolate    == other.interpolate)   &&
                    (filename       == other.filename)      &&
                    (dimension      == other.dimension )    &&
                    (brighten       == other.brighten);
            }

            public override bool Equals(object obj) {
                TextureArgs other = obj as TextureArgs;
                if (other == null) {
                    return false;
                }
                return equals(other);
            }

            public override int GetHashCode() {
                return (int)hashCode();
            }
        }


        private Table<TextureArgs, ReferenceCountedPointer<Texture>> cache =
            new Table<TextureArgs, ReferenceCountedPointer<Texture>>();

        private long size;
        private long sizeHint;

        /**
         Checks to see if we have exceeded sizeHint, if so we will
         remove as many old entries as needed.
        */
        private void checkCacheSize() {

            if (size <= sizeHint) {
                return;
            }

            Array<TextureArgs> staleEntry = new Array<TextureArgs>();
            getStaleEntries(staleEntry);

            //To avoid thrashing we we remove entries in a random order
            staleEntry.randomize();

            while ((size > sizeHint) && (staleEntry.size() > 0)) {
                size -= cache[staleEntry.last()].pointer().sizeInMemory();
                Debug.debugPrintf(" CheckCacheSize is removing %s   \n" , staleEntry.last().filename);
                removeEntry(staleEntry.pop());
            }
        }

        /**
          Removes an entry from the cache and releases the cache's reference
          to the texture.
         */
        private void removeEntry(TextureArgs args) {
            ReferenceCountedPointer<Texture> texture = null;
            cache.get(args, out texture);
            cache.remove(args);
            if (texture != null) {
                texture.Dispose();
            }
        }

        /**
          A stale entry is a texture with no external references to it.
          This returns all of the stale entries in the cache.
         */
        private void getStaleEntries(Array<TextureArgs> staleEntry) {
            Table<TextureArgs, ReferenceCountedPointer<Texture>>.Iterator current = cache.begin();
            Table<TextureArgs, ReferenceCountedPointer<Texture>>.Iterator end = cache.end();
            // We cannot modify the hash table while we iterate over it.
            // So we create a list of entries to be removed.
            while (current != end) {
                Table<TextureArgs, ReferenceCountedPointer<Texture>>.Entry entry = current.Current;
                if (entry.value.isLastReference()) {
                    staleEntry.append(entry.key);
                }
                current.MoveNext();
            }
        }

        public TextureManager(long _sizeHint) {
            size = 0;
            sizeHint = _sizeHint;
        }

        public TextureManager() : this(10 * 1024 * 1024) {}

        /**
         If the texture has recently been loaded with the same options, a
         reference to the shared texture is returned.  Otherwise the
         texture is loaded from disk.
        */
        public ReferenceCountedPointer<Texture> loadTexture(
            string                  filename,
            TextureFormat           desiredFormat,
            Texture.WrapMode        wrap,
            Texture.InterpolateMode interpolate,
            Texture.Dimension       dimension,
            double                  brighten)
        {
            TextureArgs args = new TextureArgs(desiredFormat);

            args.filename       = filename;
            args.wrap           = wrap;
            args.interpolate    = interpolate;
            args.dimension      = dimension;
            args.brighten       = brighten;

            if (! cache.containsKey(args)) {
                // Not in the cache, so we must load it
                Texture texture = null;

                Texture.Settings settings = new Texture.Settings();
                settings.wrapMode = wrap;
                settings.interpolateMode = interpolate;

                Texture.PreProcess process = new Texture.PreProcess();
                process.brighten = (float)brighten;

                texture = Texture.fromFile(
                    filename,
                    desiredFormat,
                    dimension,
                    settings,
                    process);

                Debug.alwaysAssertM(texture != null, "Texture not found " + filename);
                cache.set(args, new ReferenceCountedPointer<Texture>(texture));
                size += texture.sizeInMemory();
                checkCacheSize();
            }
            return new ReferenceCountedPointer<Texture>(cache[args]);
        }

        public ReferenceCountedPointer<Texture> loadTexture(
            string                  filename,
            TextureFormat           desiredFormat,
            Texture.WrapMode        wrap,
            Texture.InterpolateMode interpolate,
            Texture.Dimension       dimension)
        {
            return loadTexture(filename, desiredFormat, wrap, interpolate, dimension, 1.0);
        }

        public ReferenceCountedPointer<Texture> loadTexture(
            string                  filename,
            TextureFormat           desiredFormat,
            Texture.WrapMode        wrap,
            Texture.InterpolateMode interpolate)
        {
            return loadTexture(filename, desiredFormat, wrap, interpolate, Texture.Dimension.DIM_2D, 1.0);
        }

        public ReferenceCountedPointer<Texture> loadTexture(
            string                  filename,
            TextureFormat           desiredFormat,
            Texture.WrapMode        wrap)
        {
            return loadTexture(filename, desiredFormat, wrap, Texture.InterpolateMode.TRILINEAR_MIPMAP, Texture.Dimension.DIM_2D, 1.0);
        }

        public ReferenceCountedPointer<Texture> loadTexture(
            string                  filename,
            TextureFormat           desiredFormat)
        {
            return loadTexture(filename, desiredFormat, Texture.WrapMode.TILE, Texture.InterpolateMode.TRILINEAR_MIPMAP, Texture.Dimension.DIM_2D, 1.0);
        }

        public ReferenceCountedPointer<Texture> loadTexture(
            string                  filename)
        {
            return loadTexture(filename, TextureFormat.AUTO, Texture.WrapMode.TILE, Texture.InterpolateMode.TRILINEAR_MIPMAP, Texture.Dimension.DIM_2D, 1.0);
        }

        /************************************************************************/
        /* Caches a texture                                                                      */
        /************************************************************************/
        public bool cacheTexture(
            ReferenceCountedPointer<Texture> texture,
            string                  filename,
            TextureFormat           desiredFormat,
            Texture.WrapMode        wrap,
            Texture.InterpolateMode interpolate,
            Texture.Dimension       dimension,
            double                  brighten)
        {
            TextureArgs args = new TextureArgs(desiredFormat);

            args.filename       = filename;
            args.wrap           = wrap;
            args.interpolate    = interpolate;
            args.dimension      = dimension;
            args.brighten       = brighten;

            if (cache.containsKey(args))
                return false;
            cache.set(args, new ReferenceCountedPointer<Texture>(texture));
            size += texture.pointer().sizeInMemory();
            checkCacheSize();
            return true;
        }

        public bool cacheTexture(
            ReferenceCountedPointer<Texture> texture,
            string                  filename,
            TextureFormat           desiredFormat,
            Texture.WrapMode        wrap,
            Texture.InterpolateMode interpolate,
            Texture.Dimension       dimension)
        {
            return cacheTexture(texture, filename, desiredFormat, wrap, interpolate, dimension, 1.0);
        }

        public bool cacheTexture(
            ReferenceCountedPointer<Texture> texture,
            string                  filename,
            TextureFormat           desiredFormat,
            Texture.WrapMode        wrap,
            Texture.InterpolateMode interpolate)
        {
            return cacheTexture(texture, filename, desiredFormat, wrap, interpolate, Texture.Dimension.DIM_2D, 1.0);
        }

        public bool cacheTexture(
            ReferenceCountedPointer<Texture> texture,
            string                  filename,
            TextureFormat           desiredFormat,
            Texture.WrapMode        wrap)
        {
            return cacheTexture(texture, filename, desiredFormat, wrap, Texture.InterpolateMode.TRILINEAR_MIPMAP, Texture.Dimension.DIM_2D, 1.0);
        }

        public bool cacheTexture(
            ReferenceCountedPointer<Texture> texture,
            string                  filename,
            TextureFormat           desiredFormat)
        {
            return cacheTexture(texture, filename, desiredFormat, Texture.WrapMode.TILE, Texture.InterpolateMode.TRILINEAR_MIPMAP, Texture.Dimension.DIM_2D, 1.0);
        }

        public bool cacheTexture(
            ReferenceCountedPointer<Texture> texture,
            string                  filename)
        {
            return cacheTexture(texture, filename, TextureFormat.AUTO, Texture.WrapMode.TILE, Texture.InterpolateMode.TRILINEAR_MIPMAP, Texture.Dimension.DIM_2D, 1.0);
        }

        /************************************************************************/
        /* Finds - but does not load - a texture                                                                      */
        /************************************************************************/
        public ReferenceCountedPointer<Texture> findTexture(
            string                  filename,
            TextureFormat           desiredFormat,
            Texture.WrapMode        wrap,
            Texture.InterpolateMode interpolate,
            Texture.Dimension       dimension,
            double                  brighten)
        {
            TextureArgs args = new TextureArgs(desiredFormat);

            args.filename       = filename;
            args.wrap           = wrap;
            args.interpolate    = interpolate;
            args.dimension      = dimension;
            args.brighten       = brighten;

            ReferenceCountedPointer<Texture> texture = null;
            cache.get(args, out texture);
            if (texture == null) {
                return null;
            }
            return new ReferenceCountedPointer<Texture>(texture);
        }

        public ReferenceCountedPointer<Texture> findTexture(
            string                  filename,
            TextureFormat           desiredFormat,
            Texture.WrapMode        wrap,
            Texture.InterpolateMode interpolate,
            Texture.Dimension       dimension)
        {
            return findTexture(filename, desiredFormat, wrap, interpolate, dimension, 1.0);
        }

        public ReferenceCountedPointer<Texture> findTexture(
            string                  filename,
            TextureFormat           desiredFormat,
            Texture.WrapMode        wrap,
            Texture.InterpolateMode interpolate)
        {
            return findTexture(filename, desiredFormat, wrap, interpolate, Texture.Dimension.DIM_2D, 1.0);
        }

        public ReferenceCountedPointer<Texture> findTexture(
            string                  filename,
            TextureFormat           desiredFormat,
            Texture.WrapMode        wrap)
        {
            return findTexture(filename, desiredFormat, wrap, Texture.InterpolateMode.TRILINEAR_MIPMAP, Texture.Dimension.DIM_2D, 1.0);
        }

        public ReferenceCountedPointer<Texture> findTexture(
            string                  filename,
            TextureFormat           desiredFormat)
        {
            return findTexture(filename, desiredFormat, Texture.WrapMode.TILE, Texture.InterpolateMode.TRILINEAR_MIPMAP, Texture.Dimension.DIM_2D, 1.0);
        }

        public ReferenceCountedPointer<Texture> findTexture(
            string                  filename)
        {
            return findTexture(filename, TextureFormat.AUTO, Texture.WrapMode.TILE, Texture.InterpolateMode.TRILINEAR_MIPMAP, Texture.Dimension.DIM_2D, 1.0);
        }

        /**
         Sets the maxiumum amount of memory that the cache will use before it starts to purge old entries.
        */
        public void setMemorySizeHint(long _size) {
            Debug.debugAssert(_size > 0 && _size < 1000 * 1024 * 1024);
            sizeHint = _size;
        }

        public long memorySizeHint() {
            return sizeHint;
        }

        /**
         Returns the sum of the sizes of the textures in the cache.
        */
        public long sizeInMemory() {
            return size;
        }

        /**
         Completely empties the contents of the cache.
         If a client program has a textureRef, that texture will remain in memory, but be reloaded from disk when load is called.
        */
        public void emptyCache() {
            Table<TextureArgs, ReferenceCountedPointer<Texture>>.Iterator current = cache.begin();
            Table<TextureArgs, ReferenceCountedPointer<Texture>>.Iterator end = cache.end();
            while (current != end) {
                current.Current.value.Dispose();
                current.MoveNext();
            }
            cache.clear();
            size = 0;
        }

        /**
         Removes any currently unreferenced textures from the cache.
        */
        public void trimCache() {
            Array<TextureArgs> staleEntry = new Array<TextureArgs>();
            getStaleEntries(staleEntry);
            for (int i = 0; i < staleEntry.length(); ++i) {
                size -= cache[staleEntry[i]].pointer().sizeInMemory();
                Debug.debugPrintf("TrimCache is removing %s   \n" , staleEntry.last().filename);
                removeEntry(staleEntry[i]);
            }
        }
    }
}
