using UnityEngine;
using UnityEngine.UI;

namespace GhostCounselor
{
    /// <summary>
    /// 고풍스러운 원형 초시계를 이루는 도형 하나를 그립니다.
    /// 이미지 에셋 없이 원형 테두리, 붉은 시간 부채꼴, 눈금, 초침을 생성합니다.
    /// </summary>
    public sealed class GhostTimerDialGraphic : Graphic
    {
        public enum DialPart { Face, Rim, RemainingWedge, Ticks, Hand }

        [SerializeField] private DialPart part;
        [SerializeField, Range(0f, 1f)] private float remaining = 1f;
        [SerializeField, Range(0f, 1f)] private float elapsed;

        public void SetState(float remainingValue, float elapsedValue)
        {
            remaining = Mathf.Clamp01(remainingValue);
            elapsed = Mathf.Clamp01(elapsedValue);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;

            switch (part)
            {
                case DialPart.Face:
                    AddDisc(vh, center, radius, 64, color);
                    break;
                case DialPart.Rim:
                    AddArc(vh, center, radius * 0.82f, radius, 0f, 360f, 64, color);
                    AddArc(vh, center, radius * 0.72f, radius * 0.76f, 0f, 360f, 64, new Color(0.27f, 0.12f, 0.08f, 1f));
                    break;
                case DialPart.RemainingWedge:
                    if (remaining > 0.001f)
                        AddWedge(vh, center, radius * 0.70f, -90f, 360f * remaining, Mathf.Max(4, Mathf.CeilToInt(48f * remaining)), color);
                    break;
                case DialPart.Ticks:
                    AddTicks(vh, center, radius * 0.77f, radius * 0.64f, 12, color);
                    break;
                case DialPart.Hand:
                    AddHand(vh, center, radius * 0.52f, elapsed * 360f - 90f, color);
                    AddDisc(vh, center, radius * 0.09f, 16, color);
                    break;
            }
        }

        private static void AddDisc(VertexHelper vh, Vector2 center, float radius, int segments, Color tint)
        {
            int middle = AddVertex(vh, center, tint);
            int first = -1;
            int previous = -1;
            for (int index = 0; index <= segments; index++)
            {
                float angle = -90f + 360f * index / segments;
                int current = AddVertex(vh, center + Direction(angle) * radius, tint);
                if (index == 0) first = current;
                if (previous >= 0) vh.AddTriangle(middle, previous, current);
                previous = current;
            }
        }

        private static void AddWedge(VertexHelper vh, Vector2 center, float radius, float startAngle, float sweep, int segments, Color tint)
        {
            int middle = AddVertex(vh, center, tint);
            int previous = -1;
            for (int index = 0; index <= segments; index++)
            {
                float angle = startAngle + sweep * index / segments;
                int current = AddVertex(vh, center + Direction(angle) * radius, tint);
                if (previous >= 0) vh.AddTriangle(middle, previous, current);
                previous = current;
            }
        }

        private static void AddArc(VertexHelper vh, Vector2 center, float innerRadius, float outerRadius, float startAngle, float sweep, int segments, Color tint)
        {
            int previousInner = -1;
            int previousOuter = -1;
            for (int index = 0; index <= segments; index++)
            {
                float angle = startAngle + sweep * index / segments;
                Vector2 direction = Direction(angle);
                int inner = AddVertex(vh, center + direction * innerRadius, tint);
                int outer = AddVertex(vh, center + direction * outerRadius, tint);
                if (previousInner >= 0)
                {
                    vh.AddTriangle(previousInner, previousOuter, outer);
                    vh.AddTriangle(previousInner, outer, inner);
                }
                previousInner = inner;
                previousOuter = outer;
            }
        }

        private static void AddTicks(VertexHelper vh, Vector2 center, float outerRadius, float innerRadius, int count, Color tint)
        {
            for (int index = 0; index < count; index++)
            {
                Vector2 direction = Direction(-90f + 360f * index / count);
                Vector2 perpendicular = new Vector2(-direction.y, direction.x) * (index % 3 == 0 ? 1.8f : 1.15f);
                Vector2 outer = center + direction * outerRadius;
                Vector2 inner = center + direction * innerRadius;
                AddQuad(vh, outer - perpendicular, outer + perpendicular, inner + perpendicular, inner - perpendicular, tint);
            }
        }

        private static void AddHand(VertexHelper vh, Vector2 center, float length, float angle, Color tint)
        {
            Vector2 direction = Direction(angle);
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * 2.5f;
            Vector2 tip = center + direction * length;
            AddQuad(vh, center - perpendicular, center + perpendicular, tip + perpendicular, tip - perpendicular, tint);
        }

        private static int AddVertex(VertexHelper vh, Vector2 position, Color tint)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = tint;
            vh.AddVert(vertex);
            return vh.currentVertCount - 1;
        }

        private static void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
        {
            int start = vh.currentVertCount;
            AddVertex(vh, a, tint); AddVertex(vh, b, tint); AddVertex(vh, c, tint); AddVertex(vh, d, tint);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static Vector2 Direction(float angle) => new(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
    }
}
