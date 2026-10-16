using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BalancedRowsLayoutGroup : LayoutGroup
{
    [SerializeField] int maxPerRow = 3;
    [SerializeField] Vector2 spacing = new Vector2(8, 8);

    List<int> GetRowCounts()
    {
        var result = new List<int>();
        int n = rectChildren.Count;
        if (n == 0) return result;

        int rows = Mathf.CeilToInt(n / (float)Mathf.Max(1, maxPerRow));
        int baseCount = n / rows;
        int extra = n % rows;

        for (int r = 0; r < rows; r++)
            result.Add(baseCount + (r < extra ? 1 : 0));
        return result;
    }

    // Largura (axis 0) = soma + espaçamentos; altura (axis 1) = maior filho
    float MeasureRow(int start, int count, int axis)
    {
        float total = 0f, max = 0f;
        for (int i = 0; i < count; i++)
        {
            float s = LayoutUtility.GetPreferredSize(rectChildren[start + i], axis);
            total += s;
            max = Mathf.Max(max, s);
        }
        return axis == 0 ? total + spacing.x * (count - 1) : max;
    }

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();

        float width = 0f;
        int start = 0;
        foreach (int count in GetRowCounts())
        {
            width = Mathf.Max(width, MeasureRow(start, count, 0));
            start += count;
        }
        width += padding.horizontal;
        SetLayoutInputForAxis(width, width, -1, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        var rows = GetRowCounts();
        float height = 0f;
        int start = 0;
        foreach (int count in rows)
        {
            height += MeasureRow(start, count, 1);
            start += count;
        }
        height += spacing.y * Mathf.Max(0, rows.Count - 1) + padding.vertical;
        SetLayoutInputForAxis(height, height, -1, 1);
    }

    public override void SetLayoutHorizontal()
    {
        int start = 0;
        foreach (int count in GetRowCounts())
        {
            float x = GetStartOffset(0, MeasureRow(start, count, 0));
            for (int i = 0; i < count; i++)
            {
                var child = rectChildren[start + i];
                float w = LayoutUtility.GetPreferredSize(child, 0);
                SetChildAlongAxis(child, 0, x, w);
                x += w + spacing.x;
            }
            start += count;
        }
    }

    public override void SetLayoutVertical()
    {
        var rows = GetRowCounts();
        float total = 0f;
        int start = 0;
        foreach (int count in rows)
        {
            total += MeasureRow(start, count, 1);
            start += count;
        }
        total += spacing.y * Mathf.Max(0, rows.Count - 1);

        float y = GetStartOffset(1, total);
        start = 0;
        foreach (int count in rows)
        {
            float rowH = MeasureRow(start, count, 1);
            for (int i = 0; i < count; i++)
            {
                var child = rectChildren[start + i];
                float h = LayoutUtility.GetPreferredSize(child, 1);
                SetChildAlongAxis(child, 1, y + (rowH - h) * 0.5f, h);
            }
            y += rowH + spacing.y;
            start += count;
        }
    }
}