using System.Text;
using Agebull.EntityModel.Config;

namespace Agebull.EntityModel.RobotCoder
{
    public sealed class EntityTsonBuilder : EntityBuilderBase
    {
        #region 基础

        protected override string ClassExtend => ":ITson";

        public override string BaseCode => $@"
        #region Tson方式序列化与反序列化
        {IoCode()}
        #endregion";

        protected override string Folder => "Tson";

        #endregion

        #region 序列化

        #region 主体代码


        /// <summary>
        ///     生成实体代码
        /// </summary>
        private string IoCode()
        {
            return $@"
        #region 序列化
{Index()}

        #region ITson
        
        /// <summary>
        /// 类型ID
        /// </summary>
        public int TypeId => TYPE_INDEX_{Entity.Name.ToUpper()};
        
        /// <summary>
        /// 安全的缓存长度
        /// </summary>
        public int SafeBufferLength => TSON_BUFFER_LEN_{Entity.Name.ToUpper()};

        /// <summary>
        /// 从TSON反序列化
        /// </summary>
        public void Deserialize(TsonDeserializer reader)
        {{
            reader.Begin();
            while (!reader.IsEof)
            {{
                int idx = reader.ReadIndex();
                switch (idx)
                {{{DeserializeCode(Entity)}
                }}
                break;//错误发生时中止
            }}
            reader.End();
        }}

        /// <summary>
        /// 序列化到Tson
        /// </summary>
        public void Serialize(TsonSerializer writer)
        {{
            writer.Begin(TYPE_INDEX_{Entity.Name.ToUpper()}, 1);
            {SerializeCode(Entity)}

            writer.End();
        }}
        #endregion
        #endregion 序列化";
        }

        #endregion

        #region 缓冲区长度计算


        private string Index()
        {
            var code = new StringBuilder();
            code.Append($@"
        #region 数据常量

        /// <summary>
        /// {Entity.Name}类型代号
        /// </summary>
        public const int TYPE_INDEX_{Entity.Name.ToUpper()} = 0x{Entity.Identity:X};

        /// <summary>
        /// {Entity.Name}类型代号
        /// </summary>
        public const int EntityId = 0x{Entity.Identity:X};");
            foreach (PropertyConfig property in Entity.CppProperty)
            {
                code.Append($@"

        /// <summary>
        /// {Entity.Caption}-{property.Caption} 的字段索引
        /// </summary>
        public const byte FIELD_INDEX_{Entity.Name.ToUpper()}_{property.Name.ToUpper()} = 0x{property.Identity:X};");
            }
            code.AppendLine($@"

        /// <summary>
        /// {Entity.Caption}的合理序列化长度
        /// </summary>
        public const int TSON_BUFFER_LEN_{Entity.Name.ToUpper()} = {GetEntitySerializeLen(Entity)};


        #endregion 数据常量");
            return code.ToString();
        }

        private static int GetEntitySerializeLen(EntityConfig entity)
        {
            int len = entity.Properties.Count + 94;
            foreach (var property in entity.CppProperty)
            {
                len += GetFieldSerializeLen(property);
            }
            return len;
        }

        private static int GetFieldSerializeLen(PropertyConfig field)
        {
            int len = 0;
            int.TryParse(field.ArrayLen, out len);

            switch (field.CsType.ToLower())
            {
                case "bool":
                case "byte":
                case "sbyte":
                    return len > 1 ? 1 + len : 1;
                case "short":
                case "ushort":
                case "char":
                case "int16":
                case "uint16":
                    return len > 1 ? 1 + len * 2 : 2;
                case "int":
                case "uint":
                case "int32":
                case "uint32":
                    return len > 1 ? 1 + len * 4 : 4;
                case "long":
                case "ulong":
                case "int64":
                case "uint64":
                    return len > 1 ? 1 + len * 8 : 8;
                case "guid":
                    return len > 1 ? 1 + len * 18 : 18;
                case "string":
                    return field.Datalen > 1 ? 1 + field.Datalen * 2 : 501;

            }
            var entity = GlobalConfig.GetEntity(field.CsType);
            if (entity != null)
            {
                return GetEntitySerializeLen(entity);
            }
            return 64;
        }

        #endregion

        private string SerializeCode(EntityConfig entity)
        {
            StringBuilder code = new StringBuilder();
            foreach (var field in entity.CppProperty)
            {
                if (!field.CanGet)
                    continue;
                if (field.EnumConfig != null)
                {
                    code.Append($@"
            //{field.Caption}
            if(this._{field.Name.ToLower()} != {field.EnumConfig.Name}.{field.EnumConfig.Items[0].Name})
            {{
                writer.WriteIndex(FIELD_INDEX_{Entity.Name.ToUpper()}_{field.Name.ToUpper()});
                writer.Write((int)this._{field.Name.ToLower()});
            }}");
                }
                else
                {
                    code.Append($@"
            //{field.Caption}
            if(!writer.IsEmpty(this._{field.Name.ToLower()}))
            {{
                writer.WriteIndex(FIELD_INDEX_{Entity.Name.ToUpper()}_{field.Name.ToUpper()});
                writer.Write(this._{field.Name.ToLower()});
            }}");
                }
            }
            return code.ToString();
        }


        private string DeserializeCode(EntityConfig entity)
        {
            StringBuilder code = new StringBuilder();
            foreach (var field in entity.CppProperty)
            {
                if (!field.CanSet)
                    continue;
                code.Append($@"
                case FIELD_INDEX_{Entity.Name.ToUpper()}_{field.Name.ToUpper()}://{field.Caption}");
                if (field.EnumConfig != null)
                {
                    code.Append($@"
                    {{
                        int val = 0;
                        reader.Read(ref val);
                        this._{field.Name.ToLower()} = ({field.EnumConfig.Name})val;
                    }}");

                }
                else
                {
                    var en = GlobalConfig.GetEntity(field.CsType);
                    if (en != null)
                    {
                        code.Append($@"
                    this._{field.Name.ToLower()} = reader.Read<{field.CsType}>();");
                    }
                    else
                    {
                        code.Append($@"
                    reader.Read(ref this._{field.Name.ToLower()});");
                    }
                }
                code.Append(@"
                    continue;");
            }
            return code.ToString();
        }

        #endregion
    }
}