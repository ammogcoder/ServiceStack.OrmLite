﻿using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests
{
    using System;

    public class TypeWithEnumAsStringAsPk
    {
        [PrimaryKey]
        public SomeEnum Id { get; set; }

//        [Default(typeof(bool), "0")]
        public bool IsDeleted { get; set; }


        [RowVersion]
        public byte[] RowVersion { get; set; }

    }

    public class EnumTests : OrmLiteTestBase
    {
        [Test]
        public void Can_use_RowVersion_on_EnumAsString_PrimaryKey()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithEnumAsStringAsPk>();

                db.Insert(new TypeWithEnumAsStringAsPk { Id = SomeEnum.Value1 });

                db.Save(new TypeWithEnumAsStringAsPk { Id = SomeEnum.Value2 });
            }
        }

        [Test]
        public void CanCreateTable()
        {
            OpenDbConnection().CreateTable<TypeWithEnum>(true);
        }

        [Test]
        public void CanStoreEnumValue()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
            using (var con = OpenDbConnection())
            {
                con.CreateTable<TypeWithEnum>(true);
                con.Insert(new TypeWithEnum { Id = 1 });
            }
        }

        [Test]
        public void CanGetEnumValue()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithEnum>();

                var obj = new TypeWithEnum { Id = 1, EnumValue = SomeEnum.Value1 };
                db.Save(obj);
                var target = db.SingleById<TypeWithEnum>(obj.Id);
                Assert.AreEqual(obj.Id, target.Id);
                Assert.AreEqual(obj.EnumValue, target.EnumValue);
            }
        }

        [Test]
        public void CanQueryByEnumValue_using_select_with_expression()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithEnum>();
                db.Save(new TypeWithEnum { Id = 1, EnumValue = SomeEnum.Value1 });
                db.Save(new TypeWithEnum { Id = 2, EnumValue = SomeEnum.Value1 });
                db.Save(new TypeWithEnum { Id = 3, EnumValue = SomeEnum.Value2 });

                var results = db.Select<TypeWithEnum>(q => q.EnumValue == SomeEnum.Value1);
                Assert.That(results.Count, Is.EqualTo(2));
                results = db.Select<TypeWithEnum>(q => q.EnumValue == SomeEnum.Value2);
                Assert.That(results.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void CanQueryByEnumValue_using_select_with_string()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithEnum>();
                db.Save(new TypeWithEnum { Id = 1, EnumValue = SomeEnum.Value1 });
                db.Save(new TypeWithEnum { Id = 2, EnumValue = SomeEnum.Value1 });
                db.Save(new TypeWithEnum { Id = 3, EnumValue = SomeEnum.Value2 });

                var target = db.Select<TypeWithEnum>(
                    "EnumValue".SqlColumn() + " = @value".PreNormalizeSql(db), new { value = SomeEnum.Value1 });

                Assert.AreEqual(2, target.Count());
            }
        }

        [Test]
        public void CanQueryByEnumValue_using_where_with_AnonType()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithEnum>();
                db.Save(new TypeWithEnum { Id = 1, EnumValue = SomeEnum.Value1 });
                db.Save(new TypeWithEnum { Id = 2, EnumValue = SomeEnum.Value1 });
                db.Save(new TypeWithEnum { Id = 3, EnumValue = SomeEnum.Value2 });

                var target = db.Where<TypeWithEnum>(new { EnumValue = SomeEnum.Value1 });

                Assert.AreEqual(2, target.Count());
            }
        }

        [Test]
        public void can_select_enum_equals_other_enum()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<DoubleState>();
                db.Insert(new DoubleState { Id = "1", State1 = DoubleState.State.OK, State2 = DoubleState.State.KO });
                db.Insert(new DoubleState { Id = "2", State1 = DoubleState.State.OK, State2 = DoubleState.State.OK });
                IEnumerable<DoubleState> doubleStates = db.Select<DoubleState>(x => x.State1 != x.State2);
                Assert.AreEqual(1, doubleStates.Count());
            }
        }

        [Test]
        public void StoresFlagEnumsAsNumericValues()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithFlagsEnum>();
                db.Insert(
                    new TypeWithFlagsEnum { Id = 1, Flags = FlagsEnum.FlagOne | FlagsEnum.FlagTwo | FlagsEnum.FlagThree });

                try
                {
                    var expectedFlags = (int)(FlagsEnum.FlagOne | FlagsEnum.FlagTwo | FlagsEnum.FlagThree);
                    Assert.That(db.Scalar<int>("SELECT Flags FROM {0} WHERE Id = 1"
                        .Fmt("TypeWithFlagsEnum".SqlTable())), Is.EqualTo(expectedFlags));
                }
                catch (FormatException)
                {
                    // Probably a string then
                    var value = db.Scalar<string>("SELECT Flags FROM TypeWithFlagsEnum WHERE Id = 1");
                    throw new Exception(string.Format("Expected integer value but got string value {0}", value));
                }
            }
        }

        [Test]
        public void Creates_int_field_for_enum_flags()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithFlagsEnum>();

                var createTableSql = db.GetLastSql().NormalizeSql();
                createTableSql.Print();

                Assert.That(createTableSql, Does.Contain("flags int"));
            }
        }

        [Test]
        public void Creates_int_field_for_EnumAsInt()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithEnumAsInt>();

                var createTableSql = db.GetLastSql().NormalizeSql();
                createTableSql.Print();

                Assert.That(createTableSql, Does.Contain("enumvalue int"));
            }
        }

        [Test]
        public void Updates_enum_flags_with_int_value()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithFlagsEnum>();

                db.Insert(new TypeWithFlagsEnum { Id = 1, Flags = FlagsEnum.FlagOne });
                db.Insert(new TypeWithFlagsEnum { Id = 2, Flags = FlagsEnum.FlagTwo });
                db.Insert(new TypeWithFlagsEnum { Id = 3, Flags = FlagsEnum.FlagOne | FlagsEnum.FlagTwo });

                db.Update(new TypeWithFlagsEnum { Id = 1, Flags = FlagsEnum.FlagThree });
                Assert.That(db.GetLastSql(), Does.Contain("=@Flags").Or.Contain("=:Flags"));
                db.GetLastSql().Print();

                db.UpdateOnly(new TypeWithFlagsEnum { Id = 1, Flags = FlagsEnum.FlagThree }, onlyFields: q => q.Flags);
                Assert.That(db.GetLastSql().NormalizeSql(), Does.Contain("=@flags"));

                var row = db.SingleById<TypeWithFlagsEnum>(1);
                Assert.That(row.Flags, Is.EqualTo(FlagsEnum.FlagThree));
            }
        }

        [Test]
        public void Updates_EnumAsInt_with_int_value()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithEnumAsInt>();

                db.Insert(new TypeWithEnumAsInt { Id = 1, EnumValue = SomeEnumAsInt.Value1 });
                db.Insert(new TypeWithEnumAsInt { Id = 2, EnumValue = SomeEnumAsInt.Value2 });
                db.Insert(new TypeWithEnumAsInt { Id = 3, EnumValue = SomeEnumAsInt.Value3 });

                db.Update(new TypeWithEnumAsInt { Id = 1, EnumValue = SomeEnumAsInt.Value1 });
                Assert.That(db.GetLastSql(), Does.Contain("=@EnumValue").Or.Contain("=:EnumValue"));
                db.GetLastSql().Print();

                db.UpdateOnly(new TypeWithEnumAsInt { Id = 1, EnumValue = SomeEnumAsInt.Value3 }, 
                    onlyFields: q => q.EnumValue);
                Assert.That(db.GetLastSql().NormalizeSql(), Does.Contain("=@enumvalue"));

                var row = db.SingleById<TypeWithEnumAsInt>(1);
                Assert.That(row.EnumValue, Is.EqualTo(SomeEnumAsInt.Value3));
            }
        }

        [Test]
        public void CanQueryByEnumValue_using_select_with_expression_enum_flags()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithFlagsEnum>();
                db.Save(new TypeWithFlagsEnum { Id = 1, Flags = FlagsEnum.FlagOne });
                db.Save(new TypeWithFlagsEnum { Id = 2, Flags = FlagsEnum.FlagOne });
                db.Save(new TypeWithFlagsEnum { Id = 3, Flags = FlagsEnum.FlagTwo });

                var results = db.Select<TypeWithFlagsEnum>(q => q.Flags == FlagsEnum.FlagOne);
                db.GetLastSql().Print();
                Assert.That(results.Count, Is.EqualTo(2));
                results = db.Select<TypeWithFlagsEnum>(q => q.Flags == FlagsEnum.FlagTwo);
                db.GetLastSql().Print();
                Assert.That(results.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void CanQueryByEnumValue_using_select_with_expression_EnumAsInt()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithEnumAsInt>();
                db.Save(new TypeWithEnumAsInt { Id = 1, EnumValue = SomeEnumAsInt.Value1 });
                db.Save(new TypeWithEnumAsInt { Id = 2, EnumValue = SomeEnumAsInt.Value1 });
                db.Save(new TypeWithEnumAsInt { Id = 3, EnumValue = SomeEnumAsInt.Value2 });

                var results = db.Select<TypeWithEnumAsInt>(q => q.EnumValue == SomeEnumAsInt.Value1);
                db.GetLastSql().Print();
                Assert.That(results.Count, Is.EqualTo(2));
                results = db.Select<TypeWithEnumAsInt>(q => q.EnumValue == SomeEnumAsInt.Value2);
                db.GetLastSql().Print();
                Assert.That(results.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void CanQueryByEnumValue_using_select_with_string_enum_flags()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithFlagsEnum>();
                db.Save(new TypeWithFlagsEnum { Id = 1, Flags = FlagsEnum.FlagOne });
                db.Save(new TypeWithFlagsEnum { Id = 2, Flags = FlagsEnum.FlagOne });
                db.Save(new TypeWithFlagsEnum { Id = 3, Flags = FlagsEnum.FlagTwo });

                var target = db.Select<TypeWithFlagsEnum>(
                    "Flags".SqlColumn() + " = @value".PreNormalizeSql(db), new { value = FlagsEnum.FlagOne });
                db.GetLastSql().Print();
                Assert.That(target.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void CanQueryByEnumValue_using_select_with_string_EnumAsInt()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithEnumAsInt>();
                db.Save(new TypeWithEnumAsInt { Id = 1, EnumValue = SomeEnumAsInt.Value1 });
                db.Save(new TypeWithEnumAsInt { Id = 2, EnumValue = SomeEnumAsInt.Value1 });
                db.Save(new TypeWithEnumAsInt { Id = 3, EnumValue = SomeEnumAsInt.Value2 });

                var target = db.Select<TypeWithEnumAsInt>(
                    "EnumValue".SqlColumn() + " = @value".PreNormalizeSql(db), new { value = SomeEnumAsInt.Value1 });
                db.GetLastSql().Print();
                Assert.That(target.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void CanQueryByEnumValue_using_where_with_AnonType_enum_flags()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithFlagsEnum>();
                db.Save(new TypeWithFlagsEnum { Id = 1, Flags = FlagsEnum.FlagOne });
                db.Save(new TypeWithFlagsEnum { Id = 2, Flags = FlagsEnum.FlagOne });
                db.Save(new TypeWithFlagsEnum { Id = 3, Flags = FlagsEnum.FlagTwo });

                var target = db.Where<TypeWithFlagsEnum>(new { Flags = FlagsEnum.FlagOne });
                db.GetLastSql().Print();
                Assert.That(target.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void CanQueryByEnumValue_using_where_with_AnonType_EnumAsInt()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithEnumAsInt>();
                db.Save(new TypeWithEnumAsInt { Id = 1, EnumValue = SomeEnumAsInt.Value1 });
                db.Save(new TypeWithEnumAsInt { Id = 2, EnumValue = SomeEnumAsInt.Value1 });
                db.Save(new TypeWithEnumAsInt { Id = 3, EnumValue = SomeEnumAsInt.Value2 });

                var target = db.Where<TypeWithEnumAsInt>(new { EnumValue = SomeEnumAsInt.Value1 });
                db.GetLastSql().Print();
                Assert.That(target.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void Does_save_Enum_with_label_by_default()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithEnum>();

                db.Insert(new TypeWithEnum { Id = 1, EnumValue = SomeEnum.Value1 });
                db.Insert(new TypeWithEnum { Id = 2, EnumValue = SomeEnum.Value2 });

                var row = db.Single<TypeWithEnum>(
                    "EnumValue".SqlColumn() + " = @value", new { value = "Value2" });

                Assert.That(row.Id, Is.EqualTo(2));
            }
        }

        [Test]
        public void Can_save_Enum_as_Integers()
        {
            using (JsConfig.With(treatEnumAsInteger: true))
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithTreatEnumAsInt>();

                db.Insert(new TypeWithTreatEnumAsInt { Id = 1, EnumValue = SomeEnumTreatAsInt.Value1 });
                db.Insert(new TypeWithTreatEnumAsInt { Id = 2, EnumValue = SomeEnumTreatAsInt.Value2 });

                var row = db.Single<TypeWithTreatEnumAsInt>(
                    "EnumValue".SqlColumn() + " = @value".PreNormalizeSql(db), new { value = "2" });

                Assert.That(row.Id, Is.EqualTo(2));
            }
        }

        [Test]
        public void Can_Select_Type_with_Nullable_Enum()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithNullableEnum>();

                db.Insert(new TypeWithNullableEnum { Id = 1, EnumValue = SomeEnum.Value1, NullableEnumValue = SomeEnum.Value2 });
                db.Insert(new TypeWithNullableEnum { Id = 2, EnumValue = SomeEnum.Value1 });

                var rows = db.Select<TypeWithNullableEnum>();
                Assert.That(rows.Count, Is.EqualTo(2));

                var row = rows.First(x => x.NullableEnumValue == null);
                Assert.That(row.Id, Is.EqualTo(2));

                rows = db.SqlList<TypeWithNullableEnum>("SELECT * FROM {0}"
                    .Fmt(typeof(TypeWithNullableEnum).Name.SqlTable()));

                row = rows.First(x => x.NullableEnumValue == null);
                Assert.That(row.Id, Is.EqualTo(2));

                rows = db.SqlList<TypeWithNullableEnum>("SELECT * FROM {0}"
                    .Fmt(typeof(TypeWithNullableEnum).Name.SqlTable()), new { Id = 2 });

                row = rows.First(x => x.NullableEnumValue == null);
                Assert.That(row.Id, Is.EqualTo(2));
            }
        }

        [Test]
        public void Can_get_Scalar_Enum()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithEnum>();

                var row = new TypeWithEnum { Id = 1, EnumValue = SomeEnum.Value2 };
                db.Insert(row);

                var someEnum = db.Scalar<SomeEnum>(db.From<TypeWithEnum>()
                    .Where(o => o.Id == row.Id)
                    .Select(o => o.EnumValue));

                Assert.That(someEnum, Is.EqualTo(SomeEnum.Value2));
            }
        }

        [Test]
        public void Can_get_Scalar_Enum_Flag()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TypeWithFlagsEnum>();

                var row = new TypeWithFlagsEnum { Id = 1, Flags = FlagsEnum.FlagTwo };
                db.Insert(row);

                row.PrintDump();

                var flagsEnum = db.Scalar<FlagsEnum>(db.From<TypeWithFlagsEnum>()
                    .Where(o => o.Id == row.Id)
                    .Select(o => o.Flags));

                Assert.That(flagsEnum, Is.EqualTo(FlagsEnum.FlagTwo));
            }
        }
    }


    public class DoubleState
    {
        public enum State
        {
            OK,
            KO
        }

        public string Id { get; set; }
        public State State1 { get; set; }
        public State State2 { get; set; }
    }

    public enum SomeEnum
    {
        Value1,
        Value2,
        Value3
    }

    public class TypeWithEnum
    {
        public int Id { get; set; }
        public SomeEnum EnumValue { get; set; }
    }

    public enum SomeEnumTreatAsInt
    {
        Value1 = 1,
        Value2 = 2,
        Value3 = 3
    }

    public class TypeWithTreatEnumAsInt
    {
        public int Id { get; set; }
        public SomeEnumTreatAsInt EnumValue { get; set; }
    }

    [Flags]
    public enum FlagsEnum
    {
        FlagOne = 0x0,
        FlagTwo = 0x01,
        FlagThree = 0x02
    }

    public class TypeWithFlagsEnum
    {
        public int Id { get; set; }
        public FlagsEnum Flags { get; set; }
    }

    [EnumAsInt]
    public enum SomeEnumAsInt
    {
        Value1 = 1,
        Value2 = 2,
        Value3 = 3,
    }

    public class TypeWithEnumAsInt
    {
        public int Id { get; set; }

        public SomeEnumAsInt EnumValue { get; set; }
    }

    public class TypeWithNullableEnum
    {
        public int Id { get; set; }
        public SomeEnum EnumValue { get; set; }
        public SomeEnum? NullableEnumValue { get; set; }
    }
}
