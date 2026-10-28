/**
  @file AABSPTree.h
  
  @maintainer Morgan McGuire, matrix@graphics3d.com
 
  @created 2004-01-11
  @edited  2005-02-24

  Copyright 2000-2005, Morgan McGuire.
  All rights reserved.
  
  */

using System;

namespace G3D
{
    /**
     A set that supports spatial queries using an axis-aligned
     BSP tree for speed.

     AABSPTree is as powerful as but more general than a Quad Tree, Oct Tree, or KD Tree,
     but less general than an unconstrained BSP tree (which is much slower
     to create).
     
     Internally, objects
     are arranged into an axis-aligned BSP-tree according to their 
     axis-aligned bounds.  This increases the cost of insertion to
     O(log n) but allows fast overlap queries.

     Note: Do not mutate any value once it has been inserted into AABSPTree. Values
     are copied interally. All AABSPTree iterators convert to pointers to constant
     values to reinforce this.

     <B>Dimensions</B>
     Although designed as a 3D-data structure, you can use the AABSPTree
     for data distributed along 2 or 1 axes by simply returning bounds
     that are always zero along one or more dimensions.

    */
    public class AABSPTree<T>
    {
        private static Func<T, AABox> boundsFunction;

        /**
         Provides bounds for a type with no built-in getBounds overload.
         */
        public static void setBoundsFunction(Func<T, AABox> function)
        {
            boundsFunction = function;
        }

        private static AABox getBounds(T value)
        {
            if (boundsFunction != null)
            {
                return boundsFunction(value);
            }

            object o = value;

            if (typeof(T) == typeof(Vector3))
            {
                return new AABox((Vector3)o);
            }

            if (typeof(T) == typeof(AABox))
            {
                return (AABox)o;
            }

            if (typeof(T) == typeof(Sphere))
            {
                AABox bounds;
                ((Sphere)o).getBounds(out bounds);
                return bounds;
            }

            if (typeof(T) == typeof(Box))
            {
                AABox bounds;
                ((Box)o).getBounds(out bounds);
                return bounds;
            }

            if (typeof(T) == typeof(Triangle))
            {
                AABox bounds;
                ((Triangle)o).getBounds(out bounds);
                return bounds;
            }

            if (typeof(T) == typeof(Capsule))
            {
                AABox bounds;
                ((Capsule)o).getBounds(out bounds);
                return bounds;
            }

            if (typeof(T) == typeof(Cylinder))
            {
                AABox bounds;
                ((Cylinder)o).getBounds(out bounds);
                return bounds;
            }

            throw new NotSupportedException(
                "AABSPTree<" + typeof(T).FullName + "> requires a bounds function; call setBoundsFunction.");
        }

        /**
         Wrapper for a value that includes a cache of its bounds.
         */
        internal class Handle
        {
            public T value;

            /** The bounds of each object are constrained to AABox::maxFinite */
            public AABox bounds;

            /** Center of bounds.  We cache this value to avoid recomputing it
                during the median sort, and because MSVC 6 std::sort goes into
                an infinite loop if we compute the midpoint on the fly (possibly
                a floating point roundoff issue, where B<A and A<B both are true).*/
            public Vector3 center;

            public Handle() { }

            public Handle(T v)
            {
                value = v;
                bounds = getBounds(v);
                bounds = bounds.intersect(AABox.MAX_FINITE);
                center = bounds.center();
            }
        }

        /** Returns the bounds of the sub array. Used by makeNode. */
        private static AABox computeBounds(
            Array<Handle> point,
            int beginIndex,
            int endIndex)
        {
            Vector3 lo = Vector3.inf();
            Vector3 hi = -lo;

            for (int p = beginIndex; p <= endIndex; ++p)
            {
                lo = lo.min(point[p].bounds.low());
                hi = hi.max(point[p].bounds.high());
            }

            return new AABox(lo, hi);
        }

        /**
         A sort predicate that returns true if the midpoint of the
         first argument is less than the midpoint of the second
         along the specified axis.

         Used by makeNode.
         */
        private class CenterLT
        {
            public Vector3.Axis sortAxis;

            public CenterLT(Vector3.Axis a)
            {
                sortAxis = a;
            }

            public int compare(Handle a, Handle b)
            {
                return a.center[(int)sortAxis].CompareTo(b.center[(int)sortAxis]);
            }
        }

        /**
           A sort predicate based on a box's location on the specified axis. Each
           box is given a value -1, 0, or 1 based on whether it is strictly less
           than, overlapping, or strictly greater than the value on the specified
           axis. This predicate compares the values for two boxes. The result is
           that the array is separated into three contiguous (though possibly empty)
           groups: less than, overlapping, or greater than.
        */
        private class PivotLT
        {
            public Vector3.Axis sortAxis;
            public float sortLocation;

            public PivotLT(Vector3.Axis a, float l)
            {
                sortAxis = a;
                sortLocation = l;
            }

            public int location(AABox box)
            {
                if (box.low()[(int)sortAxis] < sortLocation)
                {
                    if (box.high()[(int)sortAxis] < sortLocation)
                    {
                        return -1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else if (box.low()[(int)sortAxis] > sortLocation)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }

            public int compare(Handle a, Handle b)
            {
                AABox A = a.bounds;
                AABox B = b.bounds;

                return location(A).CompareTo(location(B));
            }
        }

        internal class Node
        {
            /** Spatial bounds on all values at this node and its children, based purely on
                the parent's splitting planes.  May be infinite */
            public AABox splitBounds;

            public Vector3.Axis splitAxis;

            /** Location along the specified axis */
            public float splitLocation;

            /** child[0] contains all values strictly 
                smaller than splitLocation along splitAxis.

                child[1] contains all values strictly
                larger.

                Both may be NULL if there are not enough
                values to bother recursing.
            */
            public Node[] child;

            /** Array of values at this node (i.e. values
                straddling the split plane + all values if
                this is a leaf node). */
            public Array<Handle> valueArray;

            /** Creates node with NULL children */
            public Node()
            {
                splitAxis = Vector3.Axis.X_AXIS;
                splitLocation = 0;
                splitBounds = new AABox(-Vector3.inf(), Vector3.inf());
                child = new Node[2];
                child[0] = null;
                child[1] = null;
                valueArray = new Array<Handle>();
            }

            /**
             Doesn't clone children.
             */
            public Node(Node other)
            {
                valueArray = new Array<Handle>(other.valueArray);
                splitAxis = other.splitAxis;
                splitLocation = other.splitLocation;
                splitBounds = other.splitBounds;
                child = new Node[2];
                child[0] = null;
                child[1] = null;
            }

            /** Copies the specified subarray of pt into point, NULLs the children.
                Assumes a second pass will set splitBounds. */
            public Node(Array<Handle> pt, int beginIndex, int endIndex)
            {
                splitAxis = Vector3.Axis.X_AXIS;
                splitLocation = 0;
                child = new Node[2];
                child[0] = null;
                child[1] = null;

                int n = endIndex - beginIndex + 1;

                valueArray = new Array<Handle>(n);
                for (int i = n - 1; i >= 0; --i)
                {
                    valueArray[i] = pt[i + beginIndex];
                }
            }

            /** Returns true if this node is a leaf (no children) */
            public bool isLeaf()
            {
                return (child[0] == null) && (child[1] == null);
            }

            /**
             Recursively appends all handles and children's handles
             to the array.
             */
            public void getHandles(Array<Handle> handleArray)
            {
                handleArray.append(valueArray);
                for (int i = 0; i < 2; ++i)
                {
                    if (child[i] != null)
                    {
                        child[i].getHandles(handleArray);
                    }
                }
            }

            public void verifyNode(Vector3 lo, Vector3 hi)
            {
                //		debugPrintf("Verifying: split %d @ %f [%f, %f, %f], [%f, %f, %f]\n",
                //			    splitAxis, splitLocation, lo.x, lo.y, lo.z, hi.x, hi.y, hi.z);

                Debug.debugAssert(lo == splitBounds.low());
                Debug.debugAssert(hi == splitBounds.high());

                for (int i = 0; i < valueArray.length(); ++i)
                {
                    AABox b = valueArray[i].bounds;

                    for (int axis = 0; axis < 3; ++axis)
                    {
                        Debug.debugAssert(b.low()[axis] <= b.high()[axis]);
                        Debug.debugAssert(b.low()[axis] > lo[axis]);
                        Debug.debugAssert(b.high()[axis] < hi[axis]);
                    }
                }

                if (child[0] != null || child[1] != null)
                {
                    Debug.debugAssert(lo[(int)splitAxis] < splitLocation);
                    Debug.debugAssert(hi[(int)splitAxis] > splitLocation);
                }

                Vector3 newLo = lo;
                newLo[(int)splitAxis] = splitLocation;
                Vector3 newHi = hi;
                newHi[(int)splitAxis] = splitLocation;

                if (child[0] != null)
                {
                    child[0].verifyNode(lo, newHi);
                }

                if (child[1] != null)
                {
                    child[1].verifyNode(newLo, hi);
                }
            }

            /**
              Stores the locations of the splitting planes (the structure but not the content)
              so that the tree can be quickly rebuilt from a previous configuration without 
              calling balance.
             */
            public static void serializeStructure(Node n, BinaryOutput bo)
            {
                if (n == null)
                {
                    bo.writeUInt8(0);
                }
                else
                {
                    bo.writeUInt8(1);
                    n.splitBounds.serialize(bo);
                    bo.writeInt32((int)n.splitAxis);
                    bo.writeFloat32(n.splitLocation);
                    for (int c = 0; c < 2; ++c)
                    {
                        serializeStructure(n.child[c], bo);
                    }
                }
            }

            /** Clears the member table */
            public static Node deserializeStructure(BinaryInput bi)
            {
                if (bi.readUInt8() == 0)
                {
                    return null;
                }
                else
                {
                    Node n = new Node();
                    n.splitBounds.deserialize(bi);
                    n.splitAxis = (Vector3.Axis)bi.readInt32();
                    n.splitLocation = bi.readFloat32();
                    for (int c = 0; c < 2; ++c)
                    {
                        n.child[c] = deserializeStructure(bi);
                    }
                    return n;
                }
            }

            /** Returns the deepest node that completely contains bounds. */
            public Node findDeepestContainingNode(AABox bounds)
            {
                // See which side of the splitting plane the bounds are on
                if (bounds.high()[(int)splitAxis] < splitLocation)
                {
                    // Bounds are on the low side.  Recurse into the child
                    // if it exists.
                    if (child[0] != null)
                    {
                        return child[0].findDeepestContainingNode(bounds);
                    }
                }
                else if (bounds.low()[(int)splitAxis] > splitLocation)
                {
                    // Bounds are on the high side, recurse into the child
                    // if it exists.
                    if (child[1] != null)
                    {
                        return child[1].findDeepestContainingNode(bounds);
                    }
                }

                // There was no containing child, so this node is the
                // deepest containing node.
                return this;
            }

            /** Appends all members that intersect the box. 
                If useSphere is true, members that pass the box test
                face a second test against the sphere. */
            public void getIntersectingMembers(
                AABox box,
                Sphere sphere,
                Array<T> members,
                bool useSphere)
            {
                // Test all values at this node
                for (int v = 0; v < valueArray.size(); ++v)
                {
                    AABox bounds = valueArray[v].bounds;
                    if (bounds.intersects(box) &&
                        (!useSphere || bounds.intersects(sphere)))
                    {
                        members.append(valueArray[v].value);
                    }
                }

                // If the left child overlaps the box, recurse into it
                if ((child[0] != null) && (box.low()[(int)splitAxis] < splitLocation))
                {
                    child[0].getIntersectingMembers(box, sphere, members, useSphere);
                }

                // If the right child overlaps the box, recurse into it
                if ((child[1] != null) && (box.high()[(int)splitAxis] > splitLocation))
                {
                    child[1].getIntersectingMembers(box, sphere, members, useSphere);
                }
            }

            /**
             Recurse through the tree, assigning splitBounds fields.
             */
            public void assignSplitBounds(AABox myBounds)
            {
                splitBounds = myBounds;

                AABox[] childBounds = new AABox[2];
                myBounds.split(splitAxis, splitLocation, out childBounds[0], out childBounds[1]);

                for (int c = 0; c < 2; ++c)
                {
                    if (child[c] != null)
                    {
                        child[c].assignSplitBounds(childBounds[c]);
                    }
                }
            }
        }

        /**
         Recursively subdivides the subarray.
         Begin and end indices are inclusive.

         Call assignSplitBounds() on the root node after making a tree.
         */
        private Node makeNode(Array<Handle> point, int beginIndex,
                       int endIndex, int valuesPerNode,
                       int numMeanSplits)
        {
            Node node = null;

            if (endIndex - beginIndex + 1 <= valuesPerNode)
            {
                // Make a new leaf node
                node = new Node(point, beginIndex, endIndex);

                // Set the pointers in the memberTable
                for (int i = beginIndex; i <= endIndex; ++i)
                {
                    memberTable.set(point[i].value, node);
                }

            }
            else
            {
                // Make a new internal node
                node = new Node();

                AABox bounds = computeBounds(point, beginIndex, endIndex);
                Vector3 extent = bounds.high() - bounds.low();

                Vector3.Axis splitAxis = extent.primaryAxis();

                double splitLocation;

                if (numMeanSplits > 0)
                {
                    // Compute the mean along the axis

                    splitLocation = (bounds.high()[(int)splitAxis] +
                                     bounds.low()[(int)splitAxis]) / 2.0;

                }
                else
                {

                    // Compute the median along the axis

                    // Sort only the subarray
                    point.sortSubArray(beginIndex, endIndex,
                        new Comparison<Handle>(new CenterLT(splitAxis).compare));

                    // Intentional integer divide
                    int midIndex = (beginIndex + endIndex) / 2;

                    // Choose the split location between the two middle elements
                    Vector3 median =
                        (point[midIndex].bounds.high() +
                         point[g3dmath.iMin(midIndex + 1,
                                point.size() - 1)].bounds.low()) * 0.5f;

                    splitLocation = median[(int)splitAxis];
                }


                // Re-sort around the split. This will allow us to easily
                // test for overlap
                point.sortSubArray(beginIndex, endIndex,
                    new Comparison<Handle>(new PivotLT(splitAxis, (float)splitLocation).compare));

                // Some number of nodes may actually overlap the midddle, so
                // they must be found and added to *this* node, not one of
                // its children
                int overlapBeginIndex, overlapEndIndex;

                for (overlapBeginIndex = beginIndex;
                     (overlapBeginIndex <= endIndex) &&
                         (point[overlapBeginIndex].bounds.high()[(int)splitAxis] <
                          splitLocation);
                     ++overlapBeginIndex) { }

                for (overlapEndIndex = endIndex;
                     (overlapEndIndex >= beginIndex) &&
                         (point[overlapEndIndex].bounds.low()[(int)splitAxis] >
                          splitLocation);
                     --overlapEndIndex) { }

                // put overlapping boxes in this node
                for (int i = overlapBeginIndex; i <= overlapEndIndex; ++i)
                {
                    node.valueArray.push(point[i]);
                    memberTable.set(point[i].value, node);
                }

                node.splitAxis = splitAxis;
                node.splitLocation = (float)splitLocation;

                if (overlapBeginIndex > beginIndex)
                {
                    node.child[0] =
                        makeNode(point, beginIndex, overlapBeginIndex - 1,
                             valuesPerNode, numMeanSplits - 1);
                }

                if (overlapEndIndex < endIndex)
                {
                    node.child[1] =
                        makeNode(point, overlapEndIndex + 1, endIndex, valuesPerNode, numMeanSplits - 1);
                }

            }

            return node;
        }

        /**
         Recursively clone the passed in node tree, setting
         pointers for members in the memberTable as appropriate.
         called by the assignment operator.
         */
        private Node cloneTree(Node src)
        {
            Node dst = new Node(src);

            // Make back pointers
            for (int i = 0; i < dst.valueArray.size(); ++i)
            {
                memberTable.set(dst.valueArray[i].value, dst);
            }

            // Clone children
            for (int i = 0; i < 2; ++i)
            {
                if (src.child[i] != null)
                {
                    dst.child[i] = cloneTree(src.child[i]);
                }
            }

            return dst;
        }

        /** Maps members to the node containing them */
        private Table<T, Node> memberTable = new Table<T, Node>();

        private Node root;

        /** To construct a balanced tree, insert the elements and then call
          AABSPTree::balance(). */
        public AABSPTree()
        {
            root = null;
        }

        public AABSPTree(AABSPTree<T> src)
        {
            root = null;
            copyFrom(src);
        }

        public AABSPTree<T> copyFrom(AABSPTree<T> src)
        {
            root = null;
            // Clone tree takes care of filling out the memberTable.
            root = cloneTree(src.root);
            return this;
        }

        public void clear()
        {
            memberTable.clear();
            root = null;
        }

        public int size()
        {
            return memberTable.size();
        }

        /**
         Inserts an object into the set if it is not
         already present.  O(log n) time.  Does not
         cause the tree to be balanced.
         */
        public void insert(T value)
        {
            if (contains(value))
            {
                // Already in the set
                return;
            }

            Handle h = new Handle(value);

            if (root == null)
            {
                // This is the first node; create a root node
                root = new Node();
            }

            Node node = root.findDeepestContainingNode(h.bounds);

            // Insert into the node
            node.valueArray.append(h);

            // Insert into the node table
            memberTable.set(value, node);
        }

        /** Inserts each elements in the array in turn.  If the tree
            begins empty (no structure and no elements), this is faster
            than inserting each element in turn.  You still need to balance
            the tree at the end.*/
        public void insert(Array<T> valueArray)
        {
            if (root == null)
            {
                // Optimized case for an empty tree; don't bother
                // searching or reallocating the root node's valueArray
                // as we incrementally insert.
                root = new Node();
                root.valueArray.resize(valueArray.size());
                for (int i = 0; i < valueArray.size(); ++i)
                {
                    // Insert in opposite order so that we have the exact same
                    // data structure as if we inserted each (i.e., order is reversed
                    // from array).
                    root.valueArray[valueArray.size() - i - 1] = new Handle(valueArray[i]);
                    memberTable.set(valueArray[i], root);
                }
            }
            else
            {
                // Insert at appropriate tree depth.
                for (int i = 0; i < valueArray.size(); ++i)
                {
                    insert(valueArray[i]);
                }
            }
        }

        /**
         Returns true if this object is in the set, otherwise
         returns false.  O(1) time.
         */
        public bool contains(T value)
        {
            return memberTable.containsKey(value);
        }

        /**
         Removes an object from the set in O(1) time.
         It is an error to remove members that are not already
         present.  May unbalance the tree.  
         
         Removing an element never causes a node (split plane) to be removed...
         nodes are only changed when the tree is rebalanced.  This behavior
         is desirable because it allows the split planes to be serialized,
         and then deserialized into an empty tree which can be repopulated.
        */
        public void remove(T value)
        {
            Debug.debugAssertM(contains(value),
                "Tried to remove an element from a " +
                "AABSPTree that was not present");

            Array<Handle> list = memberTable[value].valueArray;

            // Find the element and remove it
            for (int i = list.length() - 1; i >= 0; --i)
            {
                if (object.Equals(list[i].value, value))
                {
                    list.fastRemove(i);
                    break;
                }
            }
            memberTable.remove(value);
        }

        /**
         If the element is in the set, it is removed.
         The element is then inserted.

         This is useful when the == and hashCode methods
         on <I>T</I> are independent of the bounds.  In
         that case, you may call update(v) to insert an
         element for the first time and call update(v)
         again every time it moves to keep the tree 
         up to date.
         */
        public void update(T value)
        {
            if (contains(value))
            {
                remove(value);
            }
            insert(value);
        }

        /**
         Rebalances the tree (slow).  Call when objects
         have moved substantially from their original positions
         (which unbalances the tree and causes the spatial
         queries to be slow).
         
         @param valuesPerNode Maximum number of elements to put at
         a node.

         @param numMeanSplits numMeanSplits = 0 gives a 
         fully axis aligned BSP-tree, where the balance operation attempts to balance
         the tree so that every splitting plane has an equal number of left
         and right children (i.e. it is a <B>median</B> split along that axis).  
         This tends to maximize average performance.  

         You can override this behavior by
         setting a number of <B>mean</B> (average) splits.  numMeanSplits = MAX_INT
         creates a full oct-tree, which tends to optimize peak performance at the expense of
         average performance.  It tends to have better clustering behavior when
         members are not uniformly distributed.
         */
        public void balance(int valuesPerNode = 5, int numMeanSplits = 3)
        {
            if (root == null)
            {
                // Tree is empty
                return;
            }

            Array<Handle> handleArray = new Array<Handle>();
            root.getHandles(handleArray);

            // Delete the old tree
            clear();

            root = makeNode(handleArray, 0, handleArray.size() - 1,
                valuesPerNode, numMeanSplits);

            // Walk the tree, assigning splitBounds.  We start with unbounded
            // space.
            root.assignSplitBounds(AABox.MAX_FINITE);

#if DEBUG
            root.verifyNode(Vector3.minFinite(), Vector3.maxFinite());
#endif
        }

        /**
         @param parentMask The mask that this node returned from culledBy.
         */
        private static void getIntersectingMembers(
            Array<Plane> plane,
            Array<T> members,
            Node node,
            uint parentMask)
        {
            int dummy = 0;

            if (parentMask == 0)
            {
                // None of these planes can cull anything
                for (int v = node.valueArray.size() - 1; v >= 0; --v)
                {
                    members.append(node.valueArray[v].value);
                }

                // Iterate through child nodes
                for (int c = 0; c < 2; ++c)
                {
                    if (node.child[c] != null)
                    {
                        getIntersectingMembers(plane, members, node.child[c], 0);
                    }
                }
            }
            else
            {

                // Test values at this node against remaining planes
                for (int v = node.valueArray.size() - 1; v >= 0; --v)
                {
                    if (!node.valueArray[v].bounds.culledBy(plane, dummy, parentMask))
                    {
                        members.append(node.valueArray[v].value);
                    }
                }

                uint childMask = 0xFFFFFF;

                // Iterate through child nodes
                for (int c = 0; c < 2; ++c)
                {
if (node.child[c] != null &&
                        !node.child[c].splitBounds.culledBy(plane, ref dummy, parentMask, out childMask))
                    {
                        // This node was not culled
                        getIntersectingMembers(plane, members, node.child[c], childMask);
                    }
                }
            }
        }

        /**
         Returns all members inside the set of planes.  
          @param members The results are appended to this array.
         */
        public void getIntersectingMembers(Array<Plane> plane, Array<T> members)
        {
            if (root == null)
            {
                return;
            }

            getIntersectingMembers(plane, members, root, 0xFFFFFF);
        }

        /**
         Typically used to find all visible
         objects inside the view frustum (see also GCamera::getClipPlanes)... i.e. all objects
         <B>not<B> culled by frustum.

         Example:
         <PRE>
            Array<Object*>  visible;
            tree.getIntersectingMembers(camera.frustum(), visible);
            // ... Draw all objects in the visible array.
         </PRE>
         @param members The results are appended to this array.
         */
        public void getIntersectingMembers(GCamera.Frustum frustum, Array<T> members)
        {
            Array<Plane> plane = new Array<Plane>();

            for (int i = 0; i < frustum.faceArray.size(); ++i)
            {
                plane.append(frustum.faceArray[i].plane);
            }

            getIntersectingMembers(plane, members);
        }

        /**
         C++ STL style iterator variable.  See beginBoxIntersection().
         The iterator overloads the -> (dereference) operator, so this acts like a pointer
         to the current member.
         */
        // This iterator turns Node::getIntersectingMembers into a
        // coroutine.  It first translates that method from recursive to
        // stack based, then captures the system state (analogous to a Scheme
        // continuation) after each element is appended to the member array,
        // and allowing the computation to be restarted.
        public class BoxIntersectionIterator
        {
            /** True if this is the "end" iterator instance */
            private bool isEnd;

            /** The box that we're testing against. */
            private AABox box;

            /** Node that we're currently looking at.  Undefined if isEnd is true. */
            private Node node;

            /** Nodes waiting to be processed */
            // We could use backpointers within the tree and careful
            // state management to avoid ever storing the stack-- but
            // it is much easier this way and only inefficient if the
            // caller uses post increment (which they shouldn't!).
            private Array<Node> stack;

            /** The next index of current->valueArray to return. 
                Undefined when isEnd is true.*/
            private int nextValueArrayIndex;

            internal BoxIntersectionIterator()
            {
                isEnd = true;
                stack = new Array<Node>();
            }

            internal BoxIntersectionIterator(AABox b, Node rootNode)
            {
                box = b;
                isEnd = (rootNode == null);
                nextValueArrayIndex = -1;
                node = rootNode;
                stack = new Array<Node>();

                // We intentionally start at the "-1" index of the current node
                // so we can use the preincrement operator to move ourselves to
                // element 0 instead of repeating all of the code from the preincrement
                // method.  Note that this might cause us to become the "end"
                // instance.
                MoveNext();
            }

            public bool isEndValue()
            {
                return isEnd;
            }

            /**
             Pre increment.
             */
            public bool MoveNext()
            {
                ++nextValueArrayIndex;

                bool foundIntersection = false;
                while (!isEnd && !foundIntersection)
                {

                    // Search for the next node if we've exhausted this one
                    while ((!isEnd) && (nextValueArrayIndex >= node.valueArray.length()))
                    {
                        // If we entered this loop, then the iterator has exhausted the elements at 
                        // node (possibly because it just switched to a child node with no members).
                        // This loop continues until it finds a node with members or reaches
                        // the end of the whole intersection search.

                        // If the right child overlaps the box, push it onto the stack for
                        // processing.
                        if ((node.child[1] != null) &&
                            (box.high()[(int)node.splitAxis] > node.splitLocation))
                        {
                            stack.push(node.child[1]);
                        }

                        // If the left child overlaps the box, push it onto the stack for
                        // processing.
                        if ((node.child[0] != null) &&
                            (box.low()[(int)node.splitAxis] < node.splitLocation))
                        {
                            stack.push(node.child[0]);
                        }

                        if (stack.length() > 0)
                        {
                            // Go on to the next node (which may be either one of the ones we 
                            // just pushed, or one from farther back the tree).
                            node = stack.pop();
                            nextValueArrayIndex = 0;
                        }
                        else
                        {
                            // That was the last node; we're done iterating
                            isEnd = true;
                        }
                    }

                    // Search for the next intersection at this node until we run out of children
                    while (!isEnd && !foundIntersection && (nextValueArrayIndex < node.valueArray.length()))
                    {
                        if (box.intersects(node.valueArray[nextValueArrayIndex].bounds))
                        {
                            foundIntersection = true;
                        }
                        else
                        {
                            ++nextValueArrayIndex;
                            // If we exhaust this node, we'll loop around the master loop 
                            // to find a new node.
                        }
                    }
                }

                return !isEnd;
            }

            /** Overloaded dereference operator so the iterator can masquerade as a pointer
                to a member */
            public T Current
            {
                get
                {
                    Debug.alwaysAssertM(!isEnd, "Can't dereference the end element of an iterator");
                    return node.valueArray[nextValueArrayIndex].value;
                }
            }
        }

        /**
         Iterates through the members that intersect the box
         */
        public BoxIntersectionIterator beginBoxIntersection(AABox box)
        {
            return new BoxIntersectionIterator(box, root);
        }

        public BoxIntersectionIterator endBoxIntersection()
        {
            // The "end" iterator instance
            return new BoxIntersectionIterator();
        }

        /**
         Appends all members whose bounds intersect the box.
         See also AABSPTree::beginBoxIntersection.
         */
        public void getIntersectingMembers(AABox box, Array<T> members)
        {
            if (root == null)
            {
                return;
            }
            root.getIntersectingMembers(box, new Sphere(Vector3.zero(), 0), members, false);
        }

        /**
          @param members The results are appended to this array.
         */
        public void getIntersectingMembers(Sphere sphere, Array<T> members)
        {
            if (root == null)
            {
                return;
            }

            AABox box;
            sphere.getBounds(out box);
            root.getIntersectingMembers(box, sphere, members, true);
        }

        /** See AABSPTree::beginRayIntersection */
        public class RayIntersectionIterator
        {
            /** The stack frame contains all the info needed to resume
                computation for the RayIntersectionIterator */

            private class StackFrame
            {
                public Node node;

                /** the total checking bounds for this frame */
                public float minTime;

                /** minTime^2 */
                public float minTime2;
                public float maxTime;

                /** what we're checking right now, either from minTime to the
                    split, or from the split to maxTime (more or less...
                    there are edge cases) */
                public float startTime;
                public float endTime;

                /** endTime^2 */
                public float endTime2;

                public int nextChild;

                /** current index into node's valueArray */
                public int valIndex;


                /** cache intersection values when they're checked on the preSide,
                   split so they don't need to be checked again after the split. */
                public Array<float> intersectionCache = new Array<float>();

                public void init(Node inNode, Ray ray,
                          float inMinTime, float inMaxTime)
                {
                    node = inNode;
                    minTime = inMinTime;
                    maxTime = inMaxTime;
                    minTime2 = minTime * minTime;
                    valIndex = -1;

                    intersectionCache.resize(node.valueArray.length());

                    if (node.child[0] == null && node.child[1] == null)
                    {
                        startTime = minTime;
                        endTime = maxTime;
                        endTime2 = maxTime * maxTime;
                        nextChild = -1;
                        return;
                    }

                    Vector3.Axis splitAxis = node.splitAxis;
                    double splitLocation = node.splitLocation;

                    // this is the time along the ray until the split.
                    // could be negative if the split is behind.
                    double splitTime =
                        (splitLocation - ray.origin[(int)splitAxis]) /
                        ray.direction[(int)splitAxis];

                    // If splitTime <= minTime we'll never reach the
                    // split, so set it to inf so as not to confuse endTime.
                    // It will be noted below that when splitTime is inf
                    // only one of this node's children will be searched
                    // (the pre child). Therefore it is critical that
                    // the correct child is gone too.
                    if (splitTime <= minTime)
                    {
                        splitTime = g3dmath.inf();
                    }

                    startTime = minTime;
                    endTime = (float)Math.Min((double)maxTime, splitTime);
                    endTime2 = endTime * endTime;

                    double rayLocation = ray.origin[(int)splitAxis] +
                        ray.direction[(int)splitAxis] * minTime;

                    if (rayLocation == splitLocation)
                    {
                        // We're right on the split. Look ahead.
                        rayLocation = ray.origin[(int)splitAxis] +
                            ray.direction[(int)splitAxis] * maxTime;
                    }

                    if (rayLocation == splitLocation)
                    {
                        // right on the split, looking exactly along
                        // it, so consider no children.
                        nextChild = -1;
                    }
                    else if (rayLocation <= splitLocation)
                    {
                        nextChild = 0;
                    }
                    else
                    {
                        nextChild = 1;
                    }
                }
            }

            /** A minimum bound on the distance to the intersection. */
            public double minDistance;

            /** A maximum bound on the distance to the intersection. */
            public double maxDistance;

            /** Counts how many bounding box intersection tests have been made so
                far. */
            public int debugCounter;

            private Ray ray;
            private bool isEnd;

            private Array<StackFrame> stack = new Array<StackFrame>();
            private int stackLength;
            private int stackIndex;
            private int breakFrameIndex;
            private bool skipAABoxTests;

            internal RayIntersectionIterator(Ray r, Node rootNode, bool skip)
            {
                minDistance = 0;
                maxDistance = g3dmath.inf();
                debugCounter = 0;
                ray = r;
                isEnd = (rootNode == null);
                stackLength = 20;
                stackIndex = 0;
                breakFrameIndex = -1;
                skipAABoxTests = skip;

                stack.resize(stackLength);
                for (int i = 0; i < stackLength; ++i)
                {
                    stack[i] = new StackFrame();
                }
                stack[stackIndex].init(rootNode, ray, 0, (float)g3dmath.inf());

                MoveNext();
            }

            /* public so we can have empty ones */
            public RayIntersectionIterator()
            {
                isEnd = true;
                stackLength = 0;
                stackIndex = 0;
                breakFrameIndex = -1;
            }

            public bool isEndValue()
            {
                return isEnd;
            }

            /**
               Marks the node where the most recent intersection occurred. If
               the iterator exhausts this node it will stop and set itself to
               the end iterator.

               Use this after you find a true intersection to stop the iterator
               from searching more than necessary.
               <B>Beta API-- subject to change</B>
            */
            // In theory this method could be smarter: the caller could pass in
            // the distance of the actual collision and the iterator would keep
            // itself from checking nodes or boxes beyond that distance.
            public void markBreakNode()
            {
                breakFrameIndex = stackIndex;
            }

            /**
               Clears the break node. Can be used before or after the iterator
               stops from a break.
               <B>Beta API-- subject to change</B>
            */
            public void clearBreakNode()
            {
                if (breakFrameIndex < 0)
                {
                    return;
                }

                if (isEnd && stackIndex >= 0)
                {
                    isEnd = false;
                }

                breakFrameIndex = -1;
            }

            public bool MoveNext()
            {
                Debug.alwaysAssertM(!isEnd, "Can't increment the end element of an iterator");

                StackFrame s = stack[stackIndex];

                // leave the loop if:
                //    end is reached (ie: stack is empty)
                //    found an intersection

                while (true)
                {
                    ++s.valIndex;

                    if (s.valIndex >= s.node.valueArray.length())
                    {
                        // This node is exhausted, look at its
                        // children.

                        Node child = (s.nextChild >= 0) ?
                            s.node.child[s.nextChild] : null;
                        double childStartTime = s.startTime;
                        double childEndTime = s.endTime;

                        if (s.endTime < s.maxTime)
                        {
                            // we can come back to this frame,
                            // so reset it
                            s.valIndex = -1;
                            s.startTime = s.endTime;
                            s.endTime = s.maxTime;
                            s.endTime2 = s.maxTime * s.maxTime;
                            s.nextChild = (s.nextChild >= 0) ?
                                (1 - s.nextChild) : -1;

                            // this could be changed somehow,
                            // since Array already does the
                            // power-of-two growth stuff
                            if (stackIndex == stackLength)
                            {
                                stackLength *= 2;
                                int oldSize = stack.size();
                                stack.resize(stackLength);
                                for (int i = oldSize; i < stackLength; ++i)
                                {
                                    stack[i] = new StackFrame();
                                }
                            }
                        }
                        else
                        {
                            // tail-recursion: we won't come
                            // back to this frame, so we can
                            // remove it.

                            if (stackIndex == breakFrameIndex)
                            {
                                // This will be the case if the
                                // break frame is set on a node, but
                                // the node is exhausted so it won't
                                // be put on the stack. Here we
                                // decrement the break frame so that
                                // the break occurs when the current
                                // frame's parent is resumed.
                                --breakFrameIndex;
                            }

                            --stackIndex;
                        }

                        // There could have been a resize on the array, so
                        // do not use s (pointer into the array)!

                        if (child != null)
                        {
                            ++stackIndex;
                            if (stackIndex >= stack.size())
                            {
                                int oldSize = stack.size();
                                stackLength = Math.Max(stackLength * 2, stackIndex + 1);
                                stack.resize(stackLength);
                                for (int i = oldSize; i < stackLength; ++i)
                                {
                                    stack[i] = new StackFrame();
                                }
                            }
                            stack[stackIndex].init(
                                child, ray,
                                (float)childStartTime, (float)childEndTime);
                        }

                        if ((stackIndex < 0) || (stackIndex == breakFrameIndex))
                        {
                            isEnd = true;
                            break;
                        }

                        s = stack[stackIndex];
                        continue;
                    }

                    if (skipAABoxTests)
                    {
                        // No AABox test-- return everything
                        minDistance = s.startTime;
                        maxDistance = s.endTime;
                        break;
                    }
                    else
                    {
                        double t2;
                        // this can be an exact equals because the two
                        // variables are initialized to the same thing
                        if (s.startTime == s.minTime)
                        {
                            Vector3 location;

                            if (
                                CollisionDetection.collisionLocationForMovingPointFixedAABox(
                                    ray.origin, ray.direction,
                                    s.node.valueArray[s.valIndex].bounds,
                                    out location))
                            {
                                // Optimization: store t-squared 
                                t2 = (location - ray.origin).squaredMagnitude();
                            }
                            else
                            {
                                t2 = g3dmath.inf();
                            }

                            //t = ray.intersectionTime(s.node.valueArray[s.valIndex].bounds);
                            s.intersectionCache[s.valIndex] = (float)t2;
                            ++debugCounter;
                        }
                        else
                        {
                            t2 = s.intersectionCache[s.valIndex];
                        }

                        // use minTime here because intersection may be
                        // detected pre-split, but be valid post-split, too.
                        if ((t2 >= s.minTime2) && (t2 < s.endTime2))
                        {
                            // Gives slightly tighter bounds but runs slower:
                            // minDistance = max(t, s->startTime);
                            minDistance = s.startTime;
                            maxDistance = s.endTime;
                            break;
                        }
                    }

                }

                return !isEnd;
            }

            /** Overloaded dereference operator so the iterator can masquerade as a pointer
                to a member */
            public T Current
            {
                get
                {
                    Debug.alwaysAssertM(!isEnd, "Can't dereference the end element of an iterator");
                    return stack[stackIndex].node.valueArray[stack[stackIndex].valIndex].value;
                }
            }
        }

        /**
          Stores the locations of the splitting planes (the structure but not the content)
          so that the tree can be quickly rebuilt from a previous configuration without 
          calling balance.
         */
        public void serializeStructure(BinaryOutput bo)
        {
            Node.serializeStructure(root, bo);
        }

        /** Clears the member table */
        public void deserializeStructure(BinaryInput bi)
        {
            clear();
            root = Node.deserializeStructure(bi);
        }

        /**
          Generates a RayIntersectionIterator that produces successive
          elements from the set whose bounding boxes are intersected by the ray.
          Typically used for ray tracing, hit-scan, and collision detection.
         
          @param skipAABoxTests Set to true when the intersection test for a
          member is faster than an AABox-ray intersection test.  In that case, 
          the iterator will not use a bounding box test on values that are
          returned.  Leave false (the default) for objects with slow intersection
          tests.  In that case, the iterator guarantees that the ray hits the
          bounds of any object returned.
         
         @cite Implementation by Pete Hopkins
        */
        public RayIntersectionIterator beginRayIntersection(Ray ray, bool skipAABoxTests = false)
        {
            return new RayIntersectionIterator(ray, root, skipAABoxTests);
        }

        public RayIntersectionIterator endRayIntersection()
        {
            return new RayIntersectionIterator();
        }


        /**
         Returns an array of all members of the set.  See also AABSPTree::begin.
         */
        public void getMembers(Array<T> members)
        {
            memberTable.getKeys(members);
        }

        /**
         C++ STL style iterator variable.  See begin().
         Overloads the -> (dereference) operator, so this acts as a pointer
         to the current member.
        */
        public class Iterator
        {
            // Note: this is a Table iterator, we are currently defining
            // Set iterator
            private Table<T, Node>.Iterator it;
            private bool atStart;

            internal Iterator(Table<T, Node>.Iterator it)
            {
                this.it = it;
                this.atStart = true;
            }

            public bool MoveNext()
            {
                if (atStart)
                {
                    atStart = false;
                    return !it.isDoneValue();
                }

                bool result = it.MoveNext();
                return result;
            }

            public T Current
            {
                get
                {
                    return it.Current.key;
                }
            }
        }

        /**
         C++ STL style iterator method.  Returns the first member.  
         Use preincrement (++entry) to get to the next element (iteration
         order is arbitrary).  
         Do not modify the set while iterating.
         */
        public Iterator begin()
        {
            return new Iterator(memberTable.begin());
        }

        /**
         C++ STL style iterator method.  Returns one after the last iterator
         element.
         */
        public Iterator end()
        {
            return new Iterator(memberTable.end());
        }
    }

    public class KDTreeSet<T> : AABSPTree<T>
    {
        public KDTreeSet() : base() { }
        public KDTreeSet(AABSPTree<T> src) : base(src) { }
    }
}




