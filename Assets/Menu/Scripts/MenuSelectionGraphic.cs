using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DormitoryMystery.Menu
{
    /// <summary>A lightweight, resolution-independent brush stroke for menu selection.</summary>
    [AddComponentMenu("UI/Menu Selection Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MenuSelectionGraphic : MaskableGraphic, IPointerEnterHandler
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            const int rows = 22;
            for (int row = 0; row < rows; row++)
            {
                float bottom = Mathf.Lerp(rect.yMin, rect.yMax, row / (float)rows);
                float top = Mathf.Lerp(rect.yMin, rect.yMax, (row + 1f) / rows);
                float left = rect.xMin + ((row * 7 + 3) % 11) * rect.width * 0.002f;
                float right = rect.xMax - ((row * 13 + 1) % 17) * rect.width * 0.002f;
                float strength = row == 0 || row == rows - 1 ? 0.4f : 0.82f + (row % 3) * 0.06f;
                AddStrip(mesh, left, Mathf.Lerp(left, right, 0.22f), bottom, top, strength * 0.8f, strength);
                AddStrip(mesh, Mathf.Lerp(left, right, 0.22f), Mathf.Lerp(left, right, 0.65f), bottom, top, strength, strength * 0.6f);
                AddStrip(mesh, Mathf.Lerp(left, right, 0.65f), right, bottom, top, strength * 0.6f, strength * 0.08f);
            }
        }

        private void AddStrip(VertexHelper mesh, float left, float right, float bottom, float top, float leftAlpha, float rightAlpha)
        {
            int index = mesh.currentVertCount;
            Color leftColor = new Color(0.66f, 0.018f, 0.02f, leftAlpha) * color;
            Color rightColor = new Color(0.4f, 0.01f, 0.012f, rightAlpha) * color;
            mesh.AddVert(new Vector3(left, bottom), leftColor, Vector2.zero);
            mesh.AddVert(new Vector3(left, top), leftColor, Vector2.up);
            mesh.AddVert(new Vector3(right, top), rightColor, Vector2.one);
            mesh.AddVert(new Vector3(right, bottom), rightColor, Vector2.right);
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index + 2, index + 3, index);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Button button = GetComponent<Button>();
            if (button != null && button.IsInteractable() && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
