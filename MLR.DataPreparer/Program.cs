using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using MLR.Domain;
using MLR.Domain.DataRows;
using MLR.Domain.Enums;

namespace MLR.DataPreparer
{
    public static class Program
    {
        private static CsvConfiguration csvConfig = new (CultureInfo.InvariantCulture) { Delimiter = ";" };

        public static void WriteList<TEntity>(List<TEntity> list)
        {
            Console.WriteLine($"\nUnique items is {list.Count}:");
            var index = 0;
            list.ForEach(x => Console.WriteLine($"{index++} {x}"));
        }

        public static void WriteDictionary(string filename, List<string> list)
        {
            var index = 0;
            var outputs = list
                .Select(x => new NamedPersistantObject() { Id = index++, Name = x })
                .ToList();

            using var writer = new StreamWriter(filename);
            using var csv = new CsvWriter(writer, csvConfig);

            csv.WriteHeader<NamedPersistantObject>();
            csv.NextRecord();
            foreach (var output in outputs)
            {
                csv.WriteRecord(output);
                csv.NextRecord();
            }
        }

        public static void Main(string[] args)
        {
            var records = new List<RawDataRow>();

            var roles = new List<string>();
            var specializations = new List<string>();
            var skills = new List<string>();

            using(var reader = new StreamReader("raw_train_data.csv"))
            {
                using(var csv = new CsvReader(reader, csvConfig))
                {
                    records = csv.GetRecords<RawDataRow>().ToList();

                    var _roles = new HashSet<string>();
                    var _specializations = new HashSet<string>();
                    var _skills = new HashSet<string>();

                    foreach (var record in records)
                    {
                        _roles.Add(record.Role);
                        _specializations.Add(record.Specialization);
                        _skills.Add(record.Skill);
                    }

                    roles = _roles.ToList();
                    specializations = _specializations.ToList();
                    skills = _skills.ToList();
                }
            }

            WriteList(roles);
            WriteList(specializations);
            WriteList(skills);

            var _positions = new HashSet<(int, int, int)>();
            var _leveledSkills = new HashSet<(int, int)>();
            foreach (var record in records)
            {
                var roleId = roles.IndexOf(record.Role);
                var specializationId = specializations.IndexOf(record.Specialization);
                var seniorityId = Seniority.TryParse(record.Seniority, out Seniority seniorityEnum) ? (int)seniorityEnum : 0;
                var skillId = skills.IndexOf(record.Skill);
                var proficiencyId = record.Proficiency;

                _positions.Add((roleId, seniorityId, specializationId));
                _leveledSkills.Add((skillId, proficiencyId));
            }

            var outputs = new List<NormalizedDataRow>();
            var positions = _positions.ToList();
            var leveledSkills = _leveledSkills.ToList();

            WriteList(positions);
            WriteList(leveledSkills);

            foreach (var record in records)
            {
                var roleId = roles.IndexOf(record.Role);
                var specializationId = specializations.IndexOf(record.Specialization);
                var seniorityId = Seniority.TryParse(record.Seniority, out Seniority seniorityEnum) ? (int)seniorityEnum : 0;
                var skillId = skills.IndexOf(record.Skill);
                var proficiencyId = record.Proficiency;

                outputs.Add(new NormalizedDataRow()
                {
                    Position = positions.IndexOf((roleId, seniorityId, specializationId)),
                    Skill = leveledSkills.IndexOf((skillId, proficiencyId)),
                });
            }

            using(var writer = new StreamWriter("train_data.csv"))
            {
                using (var csv = new CsvWriter(writer, csvConfig))
                {
                    csv.WriteHeader<NormalizedDataRow>();
                    csv.NextRecord();
                    foreach (var output in outputs)
                    {
                        csv.WriteRecord(output);
                        csv.NextRecord();
                    }
                }
            }

            WriteDictionary("positions.csv", positions
                .Select(x => $"{roles[x.Item1]}, {(Seniority)x.Item2}, {specializations[x.Item3]}").ToList());
            WriteDictionary("skills.csv", leveledSkills
                .Select(x => $"{skills[x.Item1]}:{(Proficiency)x.Item2}").ToList());
        }
    }
}