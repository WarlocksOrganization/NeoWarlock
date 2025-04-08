using System;
using System.Collections.Generic;
using System.Linq;
using GameManagement.Data;
using UnityEngine;

public class CardEvaluator
{
    public Dictionary<int, double[]> CardOpen(
        List<int> decks,
        List<int> openCards,
        List<MatrixDocument> docs,
        int classCode)
    {
        Dictionary<int, double[]> results = new();

        MatrixDocument doc = docs.FirstOrDefault(d =>
            d.type == "T" && d.id.Split('/').Length > 3 && d.id.Split('/')[3] == classCode.ToString());

        if (doc == null)
        {
            throw new Exception("[CardEvaluator] 매트릭스 문서를 찾을 수 없습니다.");
        }

        if (doc.matrixMap == null)
        {
            throw new Exception("[CardEvaluator] MatrixMap이 null입니다. 매트릭스 데이터 파싱 실패 또는 JSON 오류.");
        }

        if (!doc.matrixMap.TryGetValue("-1/-1", out var matrix))
        {
            throw new Exception("[CardEvaluator] MatrixMap에 '-1/-1' 키가 없습니다.");
        }

        List<int> mergedList = new();
        mergedList.AddRange(decks);
        mergedList.AddRange(openCards);

        HashSet<int> replaceCardPool = new(doc.cardPool);
        replaceCardPool.ExceptWith(mergedList);

        // var matrix = doc.matrixMap["-1/-1"];
        var cardPoolIndex = doc.cardPool
            .Select((cardId, idx) => new { cardId, idx })
            .ToDictionary(x => x.cardId, x => x.idx);

        foreach (int openCard in openCards)
        {
            double[] result = new double[2];
            List<double> scoreMeanList = new();

            double scoreMean = CalculateScoreMean(mergedList, openCard, openCard, matrix, cardPoolIndex);
            scoreMeanList.Add(scoreMean);
            result[0] = scoreMean;

            foreach (int replaceCard in replaceCardPool)
            {
                double localScore = CalculateScoreMean(mergedList, openCard, replaceCard, matrix, cardPoolIndex);
                scoreMeanList.Add(localScore);
            }

            int greaterCount = scoreMeanList.Count(s => s > scoreMean);
            result[1] = scoreMeanList.Count == 0 ? 0 : (double)greaterCount / scoreMeanList.Count;

            results[openCard] = result;
        }

        return results;
    }

public double CalculateScoreMean(
    List<int> mergedList,
    int openCard,
    int selectCard,
    List<List<int>> matrix,
    Dictionary<int, int> cardPoolIndex)
    {
        List<double> scoreList = new();

        foreach (int other in mergedList)
        {
            if (other == openCard) continue;

            if (!cardPoolIndex.TryGetValue(other, out int row))
            {
                Debug.LogError($"[CardEvaluator] row 대상 카드 ID {other} 가 cardPoolIndex에 없습니다.");
                continue;
            }

            if (!cardPoolIndex.TryGetValue(selectCard, out int col))
            {
                Debug.LogError($"[CardEvaluator] col 대상 카드 ID {selectCard} 가 cardPoolIndex에 없습니다.");
                continue;
            }

            if (row >= matrix.Count || col >= matrix[row].Count)
            {
                Debug.LogError($"[CardEvaluator] 행/열 인덱스가 매트릭스 범위를 초과했습니다: row={row}, col={col}");
                continue;
            }

            int up = matrix[row][col];
            int down = matrix[row][row];

            scoreList.Add(down == 0 ? 0 : (double)up / down);
        }

        return scoreList.Count == 0 ? 0 : scoreList.Average();
    }

}