﻿using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using ClosedXML.Graphics;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using SixLabors.Fonts;
using WB.Core.BoundedContexts.Designer.DataAccess;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.Translations;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.SurveySolutions.ReusableCategories;
using WB.Infrastructure.Native.Questionnaire;
using WB.Infrastructure.Native.Utils;
using WB.Tests.Abc;

namespace WB.Tests.Unit.Designer.Services
{
    [TestOf(typeof(ReusableCategoriesService))]
    internal class CategoriesServiceTests
    {
        private const string NotoSansFontFamilyName = "Noto Sans";

        private static ReusableCategoriesService CreateCategoriesService(DesignerDbContext dbContext = null, 
            IQuestionnaireViewFactory questionnaireStorage = null)
        {
            return new ReusableCategoriesService(
                dbContext: dbContext ?? Mock.Of<DesignerDbContext>(),
                questionnaireStorage: questionnaireStorage ?? Mock.Of<IQuestionnaireViewFactory>(),
                categoriesExtractFactory: Create.CategoriesExtractFactory());
        }

        private static Stream CreateFileWithHeader(string[][] data, CategoriesFileType type)
        {
            var listOfData = data.ToList();

            listOfData.Insert(0, new[] {"value", "title", "parentvalue"});
            
            return CreateFile(listOfData.ToArray(), type);
        }

        private static Stream CreateFile(string[][] data, CategoriesFileType type)
        {
            switch (type)
            {
                case CategoriesFileType.Excel:
                    return CreateExcelFile(data);
                case CategoriesFileType.Tsv:
                    return CreateTsvFile(data);
                default:
                    throw new NotSupportedException();
            }
        }

        private static Stream CreateExcelFile(string[][] data)
        {
            var loadOptions = new LoadOptions { GraphicEngine = new DefaultGraphicEngine(FontsHelper.DefaultFontName) };

            
            using XLWorkbook package = new XLWorkbook(loadOptions);
            
            var worksheet = package.Worksheets.Add("Categories");

            for (var row = 0; row < data.Length; row++)
                for (var column = 0; column < data[row].Length; column++) {
                    worksheet.Cell(row + 1, column + 1).Value = data[row][column];
                    worksheet.Cell(row + 1, column + 1).Style.Font.FontName = fontForGraphicEngine.Name;
                }
            package.Style.Font.FontName = fontForGraphicEngine.Name;     

            var ms = new MemoryStream();
            package.SaveAs(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        private static Stream CreateTsvFile(string[][] data)
        {
            var ms = new MemoryStream();

            using (var sw = new StreamWriter(ms, Encoding.UTF8, 4096, true))
            using (var csvWriter = new CsvWriter(sw, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                TrimOptions = TrimOptions.Trim,
                Mode = CsvMode.RFC4180,
                Delimiter = "\t"

            }, true))
            {
                foreach (var row in data)
                {
                    csvWriter.WriteField(row);
                    csvWriter.NextRecord();
                }

                sw.Flush();
                ms.Position = 0;
            }

            return ms;
        }

        [TestCase(CategoriesFileType.Excel)]
        [TestCase(CategoriesFileType.Tsv)]
        public void when_store_and_excel_file_has_more_than_15000_categories_then_should_throw_excel_exception(CategoriesFileType type)
        {
            // arrange
            var questionnaireId = Id.g1;
            var categoriesId = Id.g2;
            var data = new string[15001].Select((x, i) => new[] {i.ToString(), $"opt {i}", ""}).ToArray();
            var service = CreateCategoriesService();

            // act
            var exception = Assert.Throws<InvalidFileException>(() =>
                service.Store(questionnaireId, categoriesId, CreateFileWithHeader(data, type), type));

            // assert
            Assert.That(exception.Message, Contains.Substring("more than 15000 categories"));
        }

        [TestCase(CategoriesFileType.Excel)]
        public void when_store_and_excel_file_hasnt_id_column_then_should_throw_excel_exception(CategoriesFileType type)
        {
            // arrange
            var questionnaireId = Id.g1;
            var categoriesId = Id.g2;
            var data = new string[][] {new[] {"text", "parentid"}};
            var service = CreateCategoriesService();

            // act
            var exception = Assert.Throws<InvalidFileException>(() =>
                service.Store(questionnaireId, categoriesId, CreateFile(data, type), type));

            // assert
            Assert.That(exception.FoundErrors, Has.One.Items);
            Assert.That(exception.FoundErrors[0].Message, Contains.Substring("value was not found"));
        }

        [TestCase(CategoriesFileType.Excel)]
        public void when_store_and_excel_file_hasnt_text_column_then_should_throw_excel_exception(CategoriesFileType type)
        {
            // arrange
            var questionnaireId = Id.g1;
            var categoriesId = Id.g2;
            var data = new string[][] {new[] {"id", "parentid"}};
            var service = CreateCategoriesService();

            // act
            var exception = Assert.Throws<InvalidFileException>(() =>
                service.Store(questionnaireId, categoriesId, CreateFile(data, type), type));

            // assert
            Assert.That(exception.FoundErrors, Has.One.Items);
            Assert.That(exception.FoundErrors[0].Message, Contains.Substring("title was not found"));
        }

        [TestCase(CategoriesFileType.Excel, "A", 2)]
        [TestCase(CategoriesFileType.Tsv, "0", 2)]
        public void when_store_excel_file_with_category_with_empty_id_then_should_throw_excel_exception(CategoriesFileType type, string expectedColumn, int expectedRow)
        {
            // arrange
            var questionnaireId = Id.g1;
            var categoriesId = Id.g2;
            var data = new string[][] {new[] {"", "option 1", "1"}};
            var service = CreateCategoriesService();

            // act
            var exception = Assert.Throws<InvalidFileException>(() =>
                service.Store(questionnaireId, categoriesId, CreateFileWithHeader(data, type), type));

            // assert
            Assert.That(exception.FoundErrors, Has.One.Items);
            Assert.That(exception.FoundErrors[0].Message, Contains.Substring($"[column: {expectedColumn}, row: {expectedRow}] Empty value"));
        }

        [TestCase(CategoriesFileType.Excel, "A", 2)]
        [TestCase(CategoriesFileType.Tsv, "0", 2)]
        public void when_store_excel_file_with_category_with_not_numeric_id_then_should_throw_excel_exception(CategoriesFileType type, string expectedColumn, int expectedRow)
        {
            // arrange
            var questionnaireId = Id.g1;
            var categoriesId = Id.g2;
            var data = new string[][] {new[] {"not numeric id", "option 1", "1"}};
            var service = CreateCategoriesService();

            // act
            var exception = Assert.Throws<InvalidFileException>(() =>
                service.Store(questionnaireId, categoriesId, CreateFileWithHeader(data, type), type));

            // assert
            Assert.That(exception.FoundErrors, Has.One.Items);
            Assert.That(exception.FoundErrors[0].Message, Contains.Substring($"[column: {expectedColumn}, row: {expectedRow}] Invalid numeric value"));
        }

        [TestCase(CategoriesFileType.Excel, "C", 2)]
        [TestCase(CategoriesFileType.Tsv, "2", 2)]
        public void when_store_excel_file_with_category_with_not_numeric_parent_id_then_should_throw_excel_exception(CategoriesFileType type, string expectedColumn, int expectedRow)
        {
            // arrange
            var questionnaireId = Id.g1;
            var categoriesId = Id.g2;
            var data = new string[][] {new[] {"1", "option 1", "not numeric id"}};
            var service = CreateCategoriesService();

            // act
            var exception = Assert.Throws<InvalidFileException>(() =>
                service.Store(questionnaireId, categoriesId, CreateFileWithHeader(data, type), type));

            // assert
            Assert.That(exception.FoundErrors, Has.One.Items);
            Assert.That(exception.FoundErrors[0].Message, Contains.Substring($"[column: {expectedColumn}, row: {expectedRow}] Invalid numeric value"));
        }

        [TestCase(CategoriesFileType.Excel, "B", 2)]
        [TestCase(CategoriesFileType.Tsv, "1", 2)]
        public void when_store_excel_file_with_category_with_empty_text_then_should_throw_excel_exception(CategoriesFileType type, string expectedColumn, int expectedRow)
        {
            // arrange
            var questionnaireId = Id.g1;
            var categoriesId = Id.g2;
            var data = new string[][] {new[] {"1", "", "1"}};
            var service = CreateCategoriesService();

            // act
            var exception = Assert.Throws<InvalidFileException>(() =>
                service.Store(questionnaireId, categoriesId, CreateFileWithHeader(data, type), type));

            // assert
            Assert.That(exception.FoundErrors, Has.One.Items);
            Assert.That(exception.FoundErrors[0].Message, Contains.Substring($"[column: {expectedColumn}, row: {expectedRow}] Empty title"));
        }

        [TestCase(CategoriesFileType.Excel)]
        [TestCase(CategoriesFileType.Tsv)]
        public void when_store_excel_file_without_categories_then_should_throw_excel_exception(CategoriesFileType type)
        {
            // arrange
            var questionnaireId = Id.g1;
            var categoriesId = Id.g2;
            var data = new string[0][];
            var service = CreateCategoriesService();

            // act
            var exception = Assert.Throws<InvalidFileException>(() =>
                service.Store(questionnaireId, categoriesId, CreateFileWithHeader(data, type), type));

            // assert
            Assert.That(exception.Message, Contains.Substring("No categories"));
        }

        [TestCase(CategoriesFileType.Excel)]
        [TestCase(CategoriesFileType.Tsv)]
        public void when_store_excel_file_with_1_category_then_should_throw_excel_exception(CategoriesFileType type)
        {
            // arrange
            var questionnaireId = Id.g1;
            var categoriesId = Id.g2;
            var data = new string[][] {new[] {"1", "option 1", "1"}};
            var service = CreateCategoriesService();

            // act
            var exception = Assert.Throws<InvalidFileException>(() =>
                service.Store(questionnaireId, categoriesId, CreateFileWithHeader(data, type), type));

            // assert
            Assert.That(exception.Message, Contains.Substring("at least 2 categories"));
        }

        [TestCase(CategoriesFileType.Excel)]
        [TestCase(CategoriesFileType.Tsv)]
        public void when_store_excel_file_with_empty_category_rows_then_should_throw_excel_exception(CategoriesFileType type)
        {
            // arrange
            var questionnaireId = Id.g1;
            var categoriesId = Id.g2;
            var data = new string[][] {new[] {"", "", ""}, new[] {"", "", ""}};
            var service = CreateCategoriesService();

            // act
            var exception = Assert.Throws<InvalidFileException>(() =>
                service.Store(questionnaireId, categoriesId, CreateFileWithHeader(data, type), type));

            // assert
            Assert.That(exception.Message, Contains.Substring("No categories"));
        }

        [TestCase(CategoriesFileType.Excel)]
        [TestCase(CategoriesFileType.Tsv)]
        public void when_store_excel_file_with_category_text_more_than_250_chars_then_should_throw_excel_exception(CategoriesFileType type)
        {
            // arrange
            var questionnaireId = Id.g1;
            var categoriesId = Id.g2;
            var data = new string[][]
            {
                new[] {"1", "option 1", "1"}, new[] {"2", "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.", "1"},
                new[] {"3", "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.", "1"}
            };
            var service = CreateCategoriesService();

            // act
            var exception = Assert.Throws<InvalidFileException>(() =>
                service.Store(questionnaireId, categoriesId, CreateFileWithHeader(data, type), type));

            // assert
            Assert.That(exception.FoundErrors, Has.Exactly(2).Items);
            Assert.That(exception.FoundErrors.Select(x => x.Message), Has.All.Contains("should be less than 250 characters"));
        }

        [TestCase(CategoriesFileType.Excel)]
        [TestCase(CategoriesFileType.Tsv)]
        public void when_store_excel_file_with_2_categories_with_parentid_and_without_then_should_throw_excel_exception(CategoriesFileType type)
        {
            // arrange
            var questionnaireId = Id.g1;
            var categoriesId = Id.g2;
            var data = new string[][]
            {
                new[] {"1", "option 1", "1"},
                new[] {"2", "option 2", ""},
            };
            var service = CreateCategoriesService();

            // act
            var exception = Assert.Throws<InvalidFileException>(() =>
                service.Store(questionnaireId, categoriesId, CreateFileWithHeader(data, type), type));

            // assert
            Assert.That(exception.Message, Contains.Substring("don't have a parent id"));
        }

        [TestCase(CategoriesFileType.Excel, new[] {2, 3})]
        [TestCase(CategoriesFileType.Tsv, new[] {2, 3})]
        public void when_store_excel_file_with_2_categories_with_the_same_id_and_parentid_then_should_throw_excel_exception(CategoriesFileType type, int[] duplicatedRows)
        {
            // arrange
            var questionnaireId = Id.g1;
            var categoriesId = Id.g2;
            var data = new string[][]
            {
                new[] {"1", "option 1", "1"},
                new[] {"1", "option 2", "1"}
            };
            var service = CreateCategoriesService();

            // act
            var exception = Assert.Throws<InvalidFileException>(() =>
                service.Store(questionnaireId, categoriesId, CreateFileWithHeader(data, type), type));

            // assert
            Assert.That(exception.FoundErrors, Has.One.Items);
            Assert.That(exception.FoundErrors[0].Message,
                Is.EqualTo($"Duplicated categories in rows: {string.Join(",", duplicatedRows)}"));
        }

        [TestCase(CategoriesFileType.Excel)]
        [TestCase(CategoriesFileType.Tsv)]
        public void when_store_excel_file_with_2_categories_and_1_empty_row_then_empty_row_should_be_ignored(CategoriesFileType type)
        {
            // arrange
            var questionnaireId = Id.g1;
            var categoriesId = Id.g2;
            var data = new[]
            {
                new[] {"1", "option 1", "1", ""}, 
                new[] {"", "", "", ""}, 
                new[] {"2", "option 2", "1", ""} 
            };
            
            var options = new DbContextOptionsBuilder<DesignerDbContext>().UseInMemoryDatabase(new Random().Next(0, 10000000).ToString());

            var designerDbContext = new DesignerDbContext(options.Options);
            var service = CreateCategoriesService(dbContext: designerDbContext);

            // act
            service.Store(questionnaireId, categoriesId, CreateFileWithHeader(data, type), type);

            designerDbContext.SaveChanges();

            Assert.That(designerDbContext.CategoriesInstances.ToList(), Has.Count.EqualTo(2));
        }

        [Test]
        public void when_store_tab_file_with_header_and_data_should_be_ok()
        {
            // arrange
            var questionnaireId = Id.g1;
            var categoriesId = Id.g2;
            var data = new string[][]
            {
                new[] {"value", "title", "parentvalue"}, 
                new[] {"1", "option 1", "1"}, 
                new[] {"", "", ""}, 
                new[] {"2", "option 2", "1"} 
            };

            var options = new DbContextOptionsBuilder<DesignerDbContext>().UseInMemoryDatabase(new Random().Next(0, 10000000).ToString());
            var designerDbContext = new DesignerDbContext(options.Options);
            var service = CreateCategoriesService(designerDbContext);
            var type = CategoriesFileType.Tsv;

            // act
            service.Store(questionnaireId, categoriesId, CreateFile(data, type), type);
            designerDbContext.SaveChanges();
            // assert
            Assert.That(designerDbContext.CategoriesInstances.ToList(), Has.Count.EqualTo(2));
        }

        [Test]
        public void when_store_excel_file_with_header_without_parentValue_and_data_should_be_ok()
        {
            // arrange
            var questionnaireId = Id.g1;
            var categoriesId = Id.g2;
            var data = new string[][]
            {
                new[] {"1", "option 1"}, 
                new[] {"", ""}, 
                new[] {"2", "option 2"} 
            };

            var options = new DbContextOptionsBuilder<DesignerDbContext>().UseInMemoryDatabase(new Random().Next(0, 10000000).ToString());
            var designerDbContext = new DesignerDbContext(options.Options);
            var service = CreateCategoriesService(designerDbContext);
            var type = CategoriesFileType.Excel;

            // act
            service.Store(questionnaireId, categoriesId, CreateFileWithHeader(data, type), type);
            designerDbContext.SaveChanges();
            // assert
            Assert.That(designerDbContext.CategoriesInstances.ToList(), Has.Count.EqualTo(2));
        }
    }
}
