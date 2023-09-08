using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using MLR.Domain;
using MLR.Domain.DataRows;

namespace MLR.Trainer
{
    public static class Program
    {
        private static CsvConfiguration csvConfig = new (CultureInfo.InvariantCulture) { Delimiter = ";" };
        private static List<NamedPersistantObject> Positions = new List<NamedPersistantObject>();
        private static List<NamedPersistantObject> Skills = new List<NamedPersistantObject>();

        public static List<NamedPersistantObject> ReadDictionary(string filename)
        {
            using var reader = new StreamReader(filename);
            using var csv = new CsvReader(reader, csvConfig);
            return csv.GetRecords<NamedPersistantObject>().ToList();
        }

        public static void Main(string[] args)
        {
            Positions = ReadDictionary("positions.csv");
            Skills = ReadDictionary("skills.csv");

            var mlContext = new MLContext();
            var trainData = mlContext.Data.LoadFromTextFile(path: "train_data.csv",
                columns: new[]
                {
                    new TextLoader.Column("Label", DataKind.Single, 0),
                    new TextLoader.Column(name:nameof(DataEntry.Position), dataKind:DataKind.UInt32, source: new [] { new TextLoader.Range(0) }, keyCount: new KeyCount((uint)Positions.Count)), 
                    new TextLoader.Column(name:nameof(DataEntry.Skill), dataKind:DataKind.UInt32, source: new [] { new TextLoader.Range(1) }, keyCount: new KeyCount((uint)Skills.Count)),
                },
                hasHeader: true,
                separatorChar: ';');

            var options = new MatrixFactorizationTrainer.Options
            {
                MatrixColumnIndexColumnName = nameof(DataEntry.Position),
                MatrixRowIndexColumnName = nameof(DataEntry.Skill),
                LabelColumnName = "Label",
                LossFunction = MatrixFactorizationTrainer.LossFunctionType.SquareLossOneClass,
                Alpha = 0.01,
                Lambda = 0.001,
                NumberOfIterations = 2000
            };

            var est = mlContext.Recommendation().Trainers.MatrixFactorization(options);
            ITransformer model = est.Fit(trainData);
            var predictionEngine = mlContext.Model.CreatePredictionEngine<DataEntry, Prediction>(model);

            for (uint position = 0; position < Positions.Count; position++)
            {
                var predictions = new List<(uint, Prediction)>();
                for (uint i = 0; i < Skills.Count; i++)
                {
                    predictions.Add((i, predictionEngine.Predict(new DataEntry() { Position = position, Skill = i })));
                }

                var maxScoredSkills = predictions.OrderByDescending(x => x.Item2.Score).Take(3).ToList();
                Console.WriteLine($"For Position = {Positions[(int)position].Name} the most scored skills is " +
                                  $"{Skills[(int)maxScoredSkills[0].Item1].Name}({maxScoredSkills[0].Item2.Score})\t" +
                                  $"{Skills[(int)maxScoredSkills[1].Item1].Name}({maxScoredSkills[1].Item2.Score})\t" +
                                  $"{Skills[(int)maxScoredSkills[2].Item1].Name}({maxScoredSkills[2].Item2.Score})");
            }
        }
    }
}
