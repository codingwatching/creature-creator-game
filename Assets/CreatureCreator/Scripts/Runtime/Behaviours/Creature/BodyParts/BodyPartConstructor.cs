﻿// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using RotaryHeart.Lib.SerializableDictionary;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DanielLochner.Assets.CreatureCreator
{
    public class BodyPartConstructor : MonoBehaviour, IFlippable<BodyPartConstructor>
    {
        #region Fields
        [SerializeField] private BodyPart bodyPart;

        [Header("Body Part")]
        [SerializeField] private Transform model;
        [SerializeField] private Light lightSource;
        [SerializeField] private SerializableDictionaryBase<StretchAxis, StretchIndices> stretchMap = new SerializableDictionaryBase<StretchAxis, StretchIndices>();

        private bool hasBodyPrimary, hasBodySecondary, hasBodyPartPrimary, hasBodyPartSecondary, isPrimaryOverridden, isSecondaryOverridden;
        #endregion

        #region Properties
        public BodyPart BodyPart
        {
            get => bodyPart;
            set
            {
                bodyPart = value;
            }
        }
        public Transform Model
        {
            get => model;
            set
            {
                model = value;
            }
        }
        public Light LightSource
        {
            get => lightSource;
        }
        public SerializableDictionaryBase<StretchAxis, StretchIndices> StretchMap
        {
            get => stretchMap;
            set
            {
                stretchMap = value;
            }
        }

        public CreatureConstructor CreatureConstructor { get; private set; }
        public BodyPartConstructor Flipped { get; set; }
        public AttachedBodyPart AttachedBodyPart { get; set; }

        public Action OnStretch { get; set; }
        public Action OnAttach { get; set; }
        public Action OnDetach { get; set; }
        public Action OnSetupAttachment { get; set; }
        public Action<Vector3> OnScale { get; set; }
        public Action<Color> OnSetPrimaryColour { get; set; }
        public Action<Color> OnSetSecondaryColour { get; set; }
        public Action<float> OnSetShine { get; set; }
        public Action<float> OnSetMetallic { get; set; }
        public Action<Renderer> OnPreOverrideMaterials { get; set; }
        public Action<Renderer> OnOverrideMaterials { get; set; }

        public Renderer Renderer { get; set; }
        public SkinnedMeshRenderer SkinnedMeshRenderer => Renderer as SkinnedMeshRenderer;
        public Material BodyPartPrimaryMat { get; private set; }
        public Material BodyPartSecondaryMat { get; private set; }

        public bool IsFlipped
        {
            get; set;
        }
        public bool IsVisible
        {
            get => gameObject.activeSelf;
        }
        public bool IsHidden
        {
            get
            {
                if (IsFlipped)
                {
                    return AttachedBodyPart.hideFlipped;
                }
                else
                {
                    return AttachedBodyPart.hideMain;
                }
            }
        }

        public bool CanOverridePrimary
        {
            get => !isPrimaryOverridden && hasBodyPrimary && !hasBodyPartPrimary;
        }
        public bool CanOverrideSecondary
        {
            get => !isSecondaryOverridden && hasBodySecondary && !hasBodyPartSecondary;
        }

        public bool CanSetPrimary
        {
            get => hasBodyPrimary || hasBodyPartPrimary;
        }
        public bool CanSetSecondary
        {
            get => hasBodySecondary || hasBodyPartSecondary;
        }

        public bool IsPrimaryOverridden
        {
            get => isPrimaryOverridden;
            set
            {
                if (isPrimaryOverridden == value)
                {
                    return;
                }
                if (value)
                {
                    OverrideMat("Body_Primary", BodyPartPrimaryMat, true);
                    Flipped.OverrideMat("Body_Primary", BodyPartPrimaryMat, true);

                    SetPrimaryColour(CreatureConstructor.Data.PrimaryColour, false);
                    SetShine(CreatureConstructor.Data.Shine, false);
                    SetMetallic(CreatureConstructor.Data.Metallic, false);
                }
                else
                {
                    OverrideMat("BodyPart_Primary", CreatureConstructor.BodyPrimaryMat, true);
                    Flipped.OverrideMat("BodyPart_Primary", CreatureConstructor.BodyPrimaryMat, true);

                    SetPrimaryColour(default, false);
                    SetShine(-1f, false);
                    SetMetallic(-1f, false);
                }

                isPrimaryOverridden = value;
            }
        }
        public bool IsSecondaryOverridden
        {
            get => isSecondaryOverridden;
            set
            {
                if (isSecondaryOverridden == value)
                {
                    return;
                }
                if (value)
                {
                    OverrideMat("Body_Secondary", BodyPartSecondaryMat, true);
                    Flipped.OverrideMat("Body_Secondary", BodyPartSecondaryMat, true);

                    SetSecondaryColour(CreatureConstructor.Data.SecondaryColour, false);
                    SetShine(CreatureConstructor.Data.Shine, false);
                    SetMetallic(CreatureConstructor.Data.Metallic, false);
                }
                else
                {
                    OverrideMat("BodyPart_Secondary", CreatureConstructor.BodySecondaryMat, true);
                    Flipped.OverrideMat("BodyPart_Secondary", CreatureConstructor.BodySecondaryMat, true);

                    SetSecondaryColour(default, false);
                    SetShine(-1f, false);
                    SetMetallic(-1f, false);
                }

                isSecondaryOverridden = value;
            }
        }

        public int NearestBone
        {
            get
            {
                int nearestBoneIndex = -1;
                float min = Mathf.Infinity;

                for (int boneIndex = 0; boneIndex < CreatureConstructor.Bones.Count; boneIndex++)
                {
                    float sqr = Vector3.SqrMagnitude(CreatureConstructor.Bones[boneIndex].position - transform.position);
                    if (sqr < min)
                    {
                        nearestBoneIndex = boneIndex;
                        min = sqr;
                    }
                }

                return nearestBoneIndex;
            }
        }
        public virtual bool CanMirror
        {
            get
            {
                return Mathf.Abs(CreatureConstructor.transform.InverseTransformPoint(transform.position).x) > CreatureConstructor.MergeThreshold;
            }
        }
        #endregion

        #region Methods
        public void Awake()
        {
            Initialize();
        }
        public void OnDestroy()
        {
            Shutdown();
        }
#if UNITY_EDITOR
        public void OnValidate()
        {
            lightSource = GetComponentInChildren<Light>(true);
        }
#endif

        public virtual void Initialize()
        {
            Renderer = model.GetComponentInChildren<Renderer>(true);
        }

        public virtual void Setup(CreatureConstructor creatureConstructor)
        {
            CreatureConstructor = creatureConstructor;

            BodyPartPrimaryMat = IsFlipped ? Flipped.BodyPartPrimaryMat : new Material(CreatureConstructor.BodyPartMaterial) { name = "BodyPart_Primary" };
            BodyPartSecondaryMat = IsFlipped ? Flipped.BodyPartSecondaryMat : new Material(CreatureConstructor.BodyPartMaterial) { name = "BodyPart_Secondary" };

            OverrideMat(null, null, false);

            if (LightSource != null)
            {
                CreatureConstructor.LightSources.Add(LightSource);

                if (CreatureConstructor.LightSources.Count > CreatureConstructor.MaxLightSources)
                {
                    LightSource.gameObject.SetActive(false);
                }
            }
        }
        public virtual void Shutdown()
        {
            if (LightSource != null)
            {
                CreatureConstructor.LightSources.Remove(LightSource);
            }
        }

        public virtual void Add()
        {
            CreatureConstructor.BodyParts.Add(this);
        }
        public virtual void Remove()
        {
            CreatureConstructor.BodyParts.Remove(this);
        }

        public virtual void Attach(AttachedBodyPart attachedBodyPart)
        {
            SetupAttachment(attachedBodyPart);
            SetAttached(attachedBodyPart);

            Flip(false);

            if (CanMirror)
            {
                gameObject.SetActive(!attachedBodyPart.hideMain);
                Flipped.gameObject.SetActive(!attachedBodyPart.hideFlipped);
            }

            OnAttach?.Invoke();
        }
        public virtual void Detach()
        {
            CreatureConstructor.RemoveBodyPart(IsFlipped ? Flipped : this);
            OnDetach?.Invoke();
        }

        public virtual void SetAttached(AttachedBodyPart abp)
        {
            // Transform
            transform.parent = CreatureConstructor.Bones[abp.boneIndex];
            transform.Set(abp.serializableTransform, CreatureConstructor.transform);

            // Stretch
            SetStretch(abp.stretch, Vector3Int.one);

            // Material
            Color primary = abp.primaryColour;
            Color secondary = abp.secondaryColour;
            float shine = abp.shine;
            float metallic = abp.metallic;

            if (primary.a != 0f)
            {
                SetPrimaryColour(primary, primary != CreatureConstructor.Data.PrimaryColour);
            }
            if (secondary.a != 0f)
            {
                SetSecondaryColour(secondary, secondary != CreatureConstructor.Data.SecondaryColour);
            }
            if (shine != -1f)
            {
                SetShine(shine, shine != CreatureConstructor.Data.Shine);
            }
            if (metallic != -1f)
            {
                SetMetallic(metallic, metallic != CreatureConstructor.Data.Metallic);
            }
        }
        public virtual void SetupAttachment(AttachedBodyPart abp)
        {
            name = abp.GUID;
            Flipped.name = name + " (Flipped)";

            CreatureConstructor.Data.AttachedBodyParts.Add(AttachedBodyPart = Flipped.AttachedBodyPart = abp);

            OnSetupAttachment?.Invoke();
        }
        public virtual void UpdateAttachmentConfiguration()
        {
            AttachedBodyPart.boneIndex = NearestBone;
            AttachedBodyPart.serializableTransform = new SerializableTransform(transform, CreatureConstructor.transform);
        }

        public virtual void Flip(bool align = true)
        {
            // Parent
            Flipped.transform.SetParent(transform.parent);

            // Position and rotation
            Vector3 localPosition = CreatureConstructor.transform.InverseTransformPoint(transform.position);
            Quaternion localRotation = Quaternion.Inverse(CreatureConstructor.transform.rotation) * transform.rotation;

            if (CanMirror)
            {
                Flipped.gameObject.SetActive(!Flipped.IsHidden);

                localPosition.x *= -1;
                Vector3 worldPosition = CreatureConstructor.transform.TransformPoint(localPosition);
                Flipped.transform.position = worldPosition;

                Quaternion worldRotation = CreatureConstructor.transform.rotation * Quaternion.Euler(localRotation.eulerAngles.x, -localRotation.eulerAngles.y, -localRotation.eulerAngles.z);
                Flipped.transform.rotation = worldRotation;
            }
            else
            {
                Flipped.gameObject.SetActive(false);

                if (align)
                {
                    localPosition.x = 0;
                    Vector3 worldPosition = CreatureConstructor.transform.TransformPoint(localPosition);
                    transform.position = Flipped.transform.position = worldPosition;

                    Vector3 localDirection = localRotation * Vector3.forward;
                    localDirection.x = 0;
                    transform.rotation = Flipped.transform.rotation = CreatureConstructor.transform.rotation * Quaternion.LookRotation(localDirection);
                }
                else
                {
                    Flipped.transform.position = transform.position;
                    Flipped.transform.rotation = transform.rotation;
                }
            }

            // Scale
            Flipped.transform.localScale = transform.localScale;

            // Stretch
            Flipped.SetStretch(AttachedBodyPart.stretch, Vector3Int.one);
        }
        public virtual void SetFlipped(BodyPartConstructor bpc)
        {
            IsFlipped = true;

            Flipped = bpc;
            bpc.Flipped = this;

            Model.localScale = new Vector3(-Model.localScale.x, Model.localScale.y, Model.localScale.z);
        }

        public void SetPrimaryColour(Color colour, bool isOverride)
        {
            if (!CanSetPrimary)
            {
                return;
            }

            if (CanOverridePrimary && isOverride)
            {
                IsPrimaryOverridden = true;
            }

            BodyPartPrimaryMat.SetColor("_Color", colour);

            AttachedBodyPart.primaryColour = colour;

            OnSetPrimaryColour?.Invoke(colour);
        }
        public void ResetPrimaryColour()
        {
            if (!CanSetPrimary)
            {
                return;
            }

            if (IsPrimaryOverridden)
            {
                IsPrimaryOverridden = false;
            }
            else
            if (!CanOverridePrimary)
            {
                Color color = BodyPart.DefaultColours.primary;
                if (color.a == 0f)
                {
                    color = CreatureConstructor.Data.PrimaryColour;
                }
                SetPrimaryColour(color, false);
            }
        }

        public void SetSecondaryColour(Color colour, bool isOverride)
        {
            if (!CanSetSecondary)
            {
                return;
            }
            if (CanOverrideSecondary && isOverride)
            {
                IsSecondaryOverridden = true;
            }

            BodyPartSecondaryMat.SetColor("_Color", colour);

            AttachedBodyPart.secondaryColour = colour;

            OnSetSecondaryColour?.Invoke(colour);
        }
        public void ResetSecondaryColour()
        {
            if (!CanSetSecondary)
            {
                return;
            }

            if (IsSecondaryOverridden)
            {
                IsSecondaryOverridden = false;
            }
            else
            if (!CanOverrideSecondary)
            {
                Color color = BodyPart.DefaultColours.secondary;
                if (color.a == 0f)
                {
                    color = CreatureConstructor.Data.SecondaryColour;
                }
                SetSecondaryColour(color, false);
            }
        }

        public void SetShine(float shine, bool isOverride)
        {
            if (isOverride)
            {
                if (CanSetPrimary && CanOverridePrimary)
                {
                    IsPrimaryOverridden = true;
                }
                if (CanSetSecondary && CanOverrideSecondary)
                {
                    IsSecondaryOverridden = true;
                }
            }

            BodyPartPrimaryMat.SetFloat("_Glossiness", shine);
            BodyPartSecondaryMat.SetFloat("_Glossiness", shine);

            AttachedBodyPart.shine = shine;

            OnSetShine?.Invoke(shine);
        }

        public void SetMetallic(float metallic, bool isOverride)
        {
            if (isOverride)
            {
                if (CanSetPrimary && CanOverridePrimary)
                {
                    IsPrimaryOverridden = true;
                }
                if (CanSetSecondary && CanOverrideSecondary)
                {
                    IsSecondaryOverridden = true;
                }
            }

            BodyPartPrimaryMat.SetFloat("_Metallic", metallic);
            BodyPartSecondaryMat.SetFloat("_Metallic", metallic);

            AttachedBodyPart.metallic = metallic;

            OnSetMetallic?.Invoke(metallic);
        }

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);

            AttachedBodyPart.serializableTransform = new SerializableTransform(transform, CreatureConstructor.transform);
        }

        public virtual void SetScale(Vector3 scale, MinMax minMaxScale)
        {
            transform.localScale = scale;
            transform.localScale = transform.localScale.Clamp(minMaxScale.min, minMaxScale.max);
            Flipped.transform.localScale = transform.localScale;

            AttachedBodyPart.serializableTransform = new SerializableTransform(transform, CreatureConstructor.transform);

            OnScale?.Invoke(scale);
        }

        public void AddStretchIndex(StretchAxis axis, bool isPositive, int index)
        {
            if (stretchMap.TryGetValue(axis, out StretchIndices indices))
            {
                if (isPositive)
                {
                    stretchMap[axis].positive = index;
                }
                else
                {
                    stretchMap[axis].negative = index;
                }
            }
            else
            {
                stretchMap.Add(axis, new StretchIndices(isPositive ? index : -1, !isPositive ? index : -1));
            }
        }
        public virtual void SetStretch(Vector3 stretch, Vector3Int mask)
        {
            stretch = stretch.Clamp(-1f, 1f).Multiply(mask) + AttachedBodyPart.stretch.Multiply(Vector3Int.one - mask);

            AttachedBodyPart.stretch = stretch;

            if (stretchMap.ContainsKey(StretchAxis.X)) SetStretchBlendShape(StretchAxis.X, stretch.x);
            if (stretchMap.ContainsKey(StretchAxis.Y)) SetStretchBlendShape(StretchAxis.Y, stretch.y);
            if (stretchMap.ContainsKey(StretchAxis.Z)) SetStretchBlendShape(StretchAxis.Z, stretch.z);

            OnStretch?.Invoke();
        }
        private void SetStretchBlendShape(StretchAxis axis, float stretch)
        {
            float weight = Mathf.Abs(stretch) * 100f;

            float positive = stretch > 0 ? weight : 0;
            float negative = stretch < 0 ? weight : 0;

            SkinnedMeshRenderer.SetBlendShapeWeight(stretchMap[axis].negative, negative);
            SkinnedMeshRenderer.SetBlendShapeWeight(stretchMap[axis].positive, positive);
        }

#region Helper
        public void OverrideMat(string matToOverride, Material overrideMat, bool notifyRenderer)
        {
            hasBodyPrimary = hasBodySecondary = hasBodyPartPrimary = hasBodyPartSecondary = false;

            if (notifyRenderer) OnPreOverrideMaterials?.Invoke(Renderer);

            Material[] materials = Renderer.materials;
            for (int j = 0; j < materials.Length; j++)
            {
                string name = TrimMatName(materials[j]);

                if (name == matToOverride)
                {
                    materials[j] = overrideMat;
                    name = overrideMat.name;
                }

                if (name == "Body_Primary")
                {
                    materials[j] = CreatureConstructor.BodyPrimaryMat;
                    hasBodyPrimary = true;
                }
                else 
                if (name == "Body_Secondary")
                {
                    materials[j] = CreatureConstructor.BodySecondaryMat;
                    hasBodySecondary = true;
                }
                else 
                if (name == "BodyPart_Primary")
                {
                    materials[j] = BodyPartPrimaryMat;
                    hasBodyPartPrimary = true;
                }
                else 
                if (name == "BodyPart_Secondary")
                {
                    materials[j] = BodyPartSecondaryMat;
                    hasBodyPartSecondary = true;
                }

                if (name != "Light")
                {
                    if (SystemUtility.IsDevice(DeviceType.Handheld))
                    {
                        if (RenderSettings.sun != null)
                        {
                            materials[j].shader = Shader.Find("Legacy Shaders/Specular");
                        }
                        else
                        {
                            materials[j].shader = Shader.Find("Legacy Shaders/VertexLit");
                        }
                    }
                    else
                    {
                        materials[j].shader = Shader.Find("Standard");
                    }

                    //materials[j].enableInstancing = true;
                }
            }
            Renderer.materials = materials;

            if (notifyRenderer) OnOverrideMaterials?.Invoke(Renderer);
        }
        private string TrimMatName(Material mat)
        {
            string name = mat.name;
            name = name.Replace("(Instance)", "");
            name = name.TrimEnd();
            return name;
        }
#endregion
#endregion

#region Enums
        [Serializable] public enum StretchAxis
        {
            X, Y, Z
        }
#endregion

#region Nested
        [Serializable] public class StretchIndices
        {
            public int negative = -1;
            public int positive = -1;

            public StretchIndices(int p, int n)
            {
                negative = n;
                positive = p;
            }
        }

        public class MatOverride
        {
            public Material mat;
            public Action<Material> onMatOverride;
        }
#endregion
    }
}