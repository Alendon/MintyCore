using System;
using System.Numerics;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Physics;
using MintyCore.Utils;
using MintyCore.Utils.UnmanagedContainers;

namespace MintyCore.Components.Common.Physic.Collisions
{
    public struct Collider : IComponent
    {
        private UnmanagedArray<ColliderContainer> _colliders;

        /// <summary>
        /// AABB in absolute World Space
        /// Offset is the point most to negative infinity
        /// Extent is the point most to positive infinity
        /// </summary>
        public AABB AABB { get; private set; }

        public byte Dirty { get; set; }

        public Identification Identification => ComponentIDs.Collider;

        public void Deserialize(DataReader reader)
        {
        }

        public void SetColliders(UnmanagedArray<ColliderContainer> colliders)
        {
            _colliders = colliders;
        }

        public void RecalculateAABB(Transform transform)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue); // most negative point
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue); // most positive point

            for (int i = 0; i < _colliders.Length; i++)
            {
                var aabb = _colliders[i].CalculateAABB(transform.Value);

                min.X = aabb.Min.X < min.X ? aabb.Min.X : min.X;
                min.Y = aabb.Min.Y < min.Y ? aabb.Min.Y : min.Y;
                min.Z = aabb.Min.Z < min.Z ? aabb.Min.Z : min.Z;

                max.X = aabb.Max.X > max.X ? aabb.Max.X : max.X;
                max.Y = aabb.Max.Y > max.Y ? aabb.Max.Y : max.Y;
                max.Z = aabb.Max.Z > max.Z ? aabb.Max.Z : max.Z;
            }

            AABB = new(min, max);
        }

        public void DecreaseRefCount()
        {
            _colliders.DecreaseRefCount();
        }

        public void IncreaseRefCount()
        {
            _colliders.IncreaseRefCount();
        }

        public void PopulateWithDefaultValues()
        {
            _colliders = default;
        }

        public void Serialize(DataWriter writer)
        {
        }
    }
}