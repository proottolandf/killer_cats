using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class VertexZoom : MonoBehaviour
{
    public float AngleMultiplier = 1.0f;
    public float SpeedMultiplier = 1.0f;
    public float CurveScale = 1.0f;

    private TMP_Text m_TextComponent;
    private bool hasTextChanged;

    void Awake()
    {
        m_TextComponent = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ON_TEXT_CHANGED);
    }

    void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ON_TEXT_CHANGED);
    }

    void Start()
    {
        StartCoroutine(AnimateVertexColors());
    }

    void ON_TEXT_CHANGED(Object obj)
    {
        if (obj == m_TextComponent)
            hasTextChanged = true;
    }

    IEnumerator AnimateVertexColors()
    {
        m_TextComponent.ForceMeshUpdate();

        TMP_TextInfo textInfo = m_TextComponent.textInfo;

        Matrix4x4 matrix;
        TMP_MeshInfo[] cachedMeshInfoVertexData = textInfo.CopyMeshInfoVertexData();

        List<float> modifiedCharScale = new List<float>();
        List<int> scaleSortingOrder = new List<int>();

        hasTextChanged = true;

        while (true)
        {
            if (hasTextChanged)
            {
                cachedMeshInfoVertexData = textInfo.CopyMeshInfoVertexData();
                hasTextChanged = false;
            }

            int characterCount = textInfo.characterCount;

            if (characterCount == 0)
            {
                yield return new WaitForSeconds(0.25f);
                continue;
            }

            modifiedCharScale.Clear();
            scaleSortingOrder.Clear();

            for (int i = 0; i < characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                if (!charInfo.isVisible)
                    continue;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                Vector3[] sourceVertices = cachedMeshInfoVertexData[materialIndex].vertices;
                Vector2 charMidBaseline = (sourceVertices[vertexIndex + 0] + sourceVertices[vertexIndex + 2]) / 2;
                Vector3 offset = charMidBaseline;

                Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

                destinationVertices[vertexIndex + 0] = sourceVertices[vertexIndex + 0] - offset;
                destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1] - offset;
                destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2] - offset;
                destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3] - offset;

                float randomScale = Random.Range(1f, 1.5f);

                modifiedCharScale.Add(randomScale);
                scaleSortingOrder.Add(modifiedCharScale.Count - 1);

                matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * randomScale);

                destinationVertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 0]);
                destinationVertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 1]);
                destinationVertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 2]);
                destinationVertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 3]);

                destinationVertices[vertexIndex + 0] += offset;
                destinationVertices[vertexIndex + 1] += offset;
                destinationVertices[vertexIndex + 2] += offset;
                destinationVertices[vertexIndex + 3] += offset;

                // Unity 2022+ compatible: uvs0 es Vector4[]
                Vector4[] sourceUVs0 = cachedMeshInfoVertexData[materialIndex].uvs0;
                Vector4[] destinationUVs0 = textInfo.meshInfo[materialIndex].uvs0;

                destinationUVs0[vertexIndex + 0] = sourceUVs0[vertexIndex + 0];
                destinationUVs0[vertexIndex + 1] = sourceUVs0[vertexIndex + 1];
                destinationUVs0[vertexIndex + 2] = sourceUVs0[vertexIndex + 2];
                destinationUVs0[vertexIndex + 3] = sourceUVs0[vertexIndex + 3];

                Color32[] sourceColors32 = cachedMeshInfoVertexData[materialIndex].colors32;
                Color32[] destinationColors32 = textInfo.meshInfo[materialIndex].colors32;

                destinationColors32[vertexIndex + 0] = sourceColors32[vertexIndex + 0];
                destinationColors32[vertexIndex + 1] = sourceColors32[vertexIndex + 1];
                destinationColors32[vertexIndex + 2] = sourceColors32[vertexIndex + 2];
                destinationColors32[vertexIndex + 3] = sourceColors32[vertexIndex + 3];
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                scaleSortingOrder.Sort((a, b) => modifiedCharScale[a].CompareTo(modifiedCharScale[b]));
                textInfo.meshInfo[i].SortGeometry(scaleSortingOrder);

                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                //textInfo.meshInfo[i].mesh.uv4 = textInfo.meshInfo[i].uvs0;
                textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;

                m_TextComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }

            yield return new WaitForSeconds(0.1f);
        }
    }
}