using System;
using System.Collections.Generic;
using System.Linq;
using GameManagement.Data;

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
            d.Type == "T" && d.Id.Split('/').Length > 3 && d.Id.Split('/')[3] == classCode.ToString());

        if (doc == null)
            throw new Exception("[CardEvaluator] 매트릭스 문서를 찾을 수 없습니다.");

        List<int> mergedList = new();
        mergedList.AddRange(decks);
        mergedList.AddRange(openCards);

        HashSet<int> replaceCardPool = new(doc.CardPool);
        replaceCardPool.ExceptWith(mergedList);

        var matrix = doc.MatrixMap["-1/-1"];
        var cardPoolIndex = doc.CardPool
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

            int row = cardPoolIndex[other];
            int col = cardPoolIndex[selectCard];

            int up = matrix[row][col];
            int down = matrix[row][row];

            scoreList.Add(down == 0 ? 0 : (double)up / down);
        }

        return scoreList.Count == 0 ? 0 : scoreList.Average();
    }
}