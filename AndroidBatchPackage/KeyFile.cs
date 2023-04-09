using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AndroidBatchPackage {

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class KeyFileNamesAttribute: Attribute {
        public List<string> KeyNames { get; private set; }
        public KeyFileNamesAttribute(params string[] keyNames) {
            KeyNames = new List<string>();
            foreach (string name in keyNames) {
                KeyNames.Add(name.ToLower().Trim());
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class CEConfigNameAttribute: Attribute {
        public string ConfigName { get; set; }
        public CEConfigNameAttribute(string configName) {
            ConfigName = configName;
        }
    }

    [AttributeUsage (AttributeTargets.Property)]
    public class CEConfigMultiLineAttribute: Attribute {

    }

    public class VipDescription {
        public int Level { get; set; }

        [KeyFileNames("_name")]
        public string Name { get; set; }

        [KeyFileNames("_description")]
        public string Description { get; set; }

        public List<string> SubscriptionId { get; set; }

        public override string ToString() {
            StringBuilder builder = new StringBuilder();
            foreach (var property in typeof(VipDescription).GetProperties()) {
                builder.AppendLine($"\t\t{property.Name}: {property.GetValue(this)}");
            }
            return builder.ToString();
        }
    }

    public class KeyFileValues {
        [KeyFileNames("应用名称")]
        [CEConfigName("c_AppName")]
        public string AppName { get; set; }

        [KeyFileNames("渠道号")]
        [CEConfigName("c_Channel")]
        public string Channel { get; set; }

        [KeyFileNames("appid")]
        [CEConfigName("c_AppId")]
        public string AppId { get; set; }

        [KeyFileNames("buddle id", "bundle id ")]
        public string BundleId { get; set; }

        [KeyFileNames("邮箱名称")]
        public string Email { get; set; }

        [KeyFileNames("友盟key")]
        [CEConfigName("c_UmengKey")]
        public string UmengKey { get; set; }

        [KeyFileNames("服务器")]
        [CEConfigName("c_ServerHost")]
        public string Server { get; set; }

        [KeyFileNames("保留语言")]
        public string ReserveLanguage { get; set; }

        // Default, X2C
        [KeyFileNames("RefractorChain")]
        public string RefractorChain { get; set; }

        [KeyFileNames("term url")]
        [CEConfigName("c_TermUrl")]
        public string TermUrl { get; set; }

        [KeyFileNames("privacy url")]
        [CEConfigName("c_PrivacyUrl")]
        public string PrivacyUrl { get; set; }

        [KeyFileNames("support url")]
        [CEConfigName("c_SupportUrl")]
        public string SupportUrl { get; set; }

        [KeyFileNames("safety tips url")]
        [CEConfigName("c_SafetyTipsUrl")]
        public string SafetyTipsUrl { get; set; }

        [KeyFileNames("version")]
        public string Version { get; set; }

        [KeyFileNames("theme")]
        public string Theme { get; set; }

        [KeyFileNames("banner_title")]
        public string BannerTitle { get; set; }

        [KeyFileNames("vip_", "zhizhun")]
        public Dictionary<int, VipDescription> VipDescriptions { get; set; }

        [KeyFileNames("订阅")]
        [CEConfigName("c_IsIAPSubscription")]
        public bool IsSubscription { get; set; } = true;

        [KeyFileNames("广告")]
        public bool HaveAds { get; set; } = false;

        /// <summary>
        /// format eg: 1BN30dys,-t1m,-p19.99,-subs; 6BN180Dys,-t6m,-p69.99,-subs
        /// </summary>
        [KeyFileNames("subscription_ids")]
        [CEConfigName("c_AutoRenewSubscriptionIapIds")]
        public string SubscriptionIds { get; set; }

        [KeyFileNames("seed")]
        public string Seed { get; set; }

        [KeyFileNames("project_name")]
        public string ProjectName { get; set; }

        [KeyFileNames("cert_public")]
        [CEConfigName("c_PubKey")]
        [CEConfigMultiLine]
        public List<string> KeyPublic { get; set; }

        [KeyFileNames("cert_private")]
        [CEConfigMultiLine]
        public List<string> KeyPrivate { get; set; }

        [KeyFileNames("android_licence_key")]
        [CEConfigName("c_AndroidLicenceKey")]
        [CEConfigMultiLine]
        public List<string> AndroidLicenceKey { get; set; }

        public string ToString(bool ignoreNull) {
            StringBuilder builder = new StringBuilder();
            foreach (var property in typeof(KeyFileValues).GetProperties()) {
                var value = property.GetValue(this);
                if (ignoreNull && (value == null)) {
                    continue;
                }
                if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
                    builder.AppendLine($"{property.Name}: ");
                    var v = value as IDictionary;
                    foreach (DictionaryEntry kv in v) {
                        builder.AppendLine($"\t{kv.Key}\n{kv.Value}");
                    }
                } else {
                    builder.AppendLine($"{property.Name}: {value}");
                }
            }
            return builder.ToString();
        }

        override public string ToString() {
            return ToString(false);
        }
    }

    public class KeyFile {
        private static readonly ILog log = LogManager.GetLogger(typeof(KeyFile));

        public string Path { get; private set; }
        public string KeyFileCulture { get; private set; }
        public KeyFileValues Values { get; private set; }
        public Dictionary<string, KeyFileValues> CultureValues { get; private set; } = new Dictionary<string, KeyFileValues>();
        public Dictionary<string, string> RawValues { get; private set; } = new Dictionary<string, string>();
        private Dictionary<string, PropertyInfo> _CEConfigProperties = new Dictionary<string, PropertyInfo>();

        public KeyFile(string path) {
            Path = path;
            if (path.Contains("英语")) {
                KeyFileCulture = "en";
            } else if (path.Contains("台湾") || path.Contains("繁体中文")) {
                KeyFileCulture = "zh-rTW";
            } else {
                throw new ArgumentException($"Please add culture resolve if KeyFile(string path) constructor for {path}");
            }
            Values = CultureValues.GetOrAddConstruct(KeyFileCulture);

            InitCEConfigProperties();
            Parse();
            Validate();
        }



        private void InitCEConfigProperties() {
            foreach (var p in typeof(KeyFileValues).GetProperties()) {
                var attr = p.GetCustomAttributes();
                foreach (var t in attr) {
                    if (t is CEConfigNameAttribute) {
                        _CEConfigProperties.Add((t as CEConfigNameAttribute).ConfigName, p);
                    }  
                }
            }
        }

        private void Parse() {
            var lines = File.ReadAllLines(Path);
            Parse(lines, CultureValues);
            if (string.IsNullOrEmpty(Values.Seed)) {
                Values.Seed = Values.Channel;
            }

            RawValues.Add("c_IsIAPSubscription", Values.IsSubscription.ToString());
            RawValues.Add("c_IsDebugingBuild", "false");

            //log.Info($"===> KeyFileCulture: [{KeyFileCulture}]");

            foreach (var culture in CultureValues) {
                log.Info($"===>[culture: {culture.Key}]");
                log.Info(culture.Value.ToString(ignoreNull: culture.Key != KeyFileCulture));
            }

            //log.Info("PubKey: " + string.Join("\" + \n\"", Values.KeyPublic) + '"');
        }

        private void Parse(string[] lines, Dictionary<string, KeyFileValues> culture) {
            Dictionary<string, PropertyInfo> proMap = new Dictionary<string, PropertyInfo>();
            foreach (var pro in typeof(KeyFileValues).GetProperties()) {
                var nameAttr = pro.GetCustomAttribute(typeof(KeyFileNamesAttribute)) as KeyFileNamesAttribute;
                if (nameAttr == null) {
                    continue;
                }
                foreach (var name in nameAttr.KeyNames) {
                    proMap.Add(name, pro);
                }
            }

            PropertyInfo p = null;
            const int stateExpectKey = 0, stateExpectValue = 1, stateValueReaded = 2;
            int state = stateExpectKey;
            string keyLine = "";
            string valueCulture = "";
            string keyName = null;
            for (int i = 0; i < lines.Length; ++i) {
                var l = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(l)) {
                    continue;
                }
                bool isKeyLine = l.EndsWith(':') || l.EndsWith("：");
                if (isKeyLine) {
                    switch (state) {
                        case stateValueReaded:
                            state = stateExpectKey;
                            p = null;
                            break;
                        case stateExpectValue:
                            throw new ArgumentException($"line {i} is not key format(end with ':'), but expect value: {l}");
                    }
                } else {
                    if (state == stateValueReaded && !l.StartsWith("@[")) {
                        throw new ArgumentException($"line {i} should be culture line('@[culture]:' format), but: {l}");
                    }
                }

                if (state == stateExpectKey) {
                    if (!isKeyLine) {
                        throw new InvalidDataException($"line {i} is not keyname format(end with ':'): {l}");
                    }
                    keyName = l.ToLower().Trim(':', '：');
                    foreach (var kv in proMap) {
                        if (keyName.StartsWith(kv.Key)) {
                            p = kv.Value;
                            break;
                        }
                    }
                    if (p == null) {
                        log.Warn($"Unknow key name: {l}");
                    }
                    state = stateExpectValue;
                    keyLine = l;
                } else {
                    if (p != null) {
                        valueCulture = KeyFileCulture;
                        if (l.StartsWith("@[")) {
                            var parts = l.Split("]:", 2);
                            valueCulture = parts[0].Trim().Trim('@', '[', ']');
                            l = parts[1].Trim();
                        }
                        if (p.Name == "VipDescriptions") {
                            SetVipDescription(keyLine, l, valueCulture, culture);
                            RawValues[keyName] = l;
                        } else {
                            object r;
                            if (p.PropertyType == typeof(int)) {
                                r = int.Parse(l);
                            } else if (p.PropertyType == typeof(bool)) {
                                r = bool.Parse(l);
                            } else if (p.PropertyType == typeof(List<string>)) {
                                r = ReadMultiLineValue(lines, ref i);
                            } else {
                                if (l.ToLower() == "none") {
                                    r = "";
                                } else {
                                    r = l;
                                }
                            }

                            p.SetValue(culture.GetOrAddConstruct(valueCulture), r);
                            RawValues[keyName] = r.ToString();
                        }
                    } else {
                        RawValues[keyName] = l;
                    }
                    state = stateValueReaded;
                }
            }
        }

        private void Validate() {
            foreach (var c in CultureValues) {
                var value = c.Value;
                var culture = c.Key;
                CheckNotNull(value.AppName, "应用名称", culture);
                CheckNotNull(value.BannerTitle, "banner_title", culture);
                CheckNotNull(value.VipDescriptions?[3].Name, "zhizhun_name", culture);
                CheckNotNull(value.VipDescriptions?[3].Description, "zhizhun_description", culture);
            }
        }

        private void CheckNotNull(object value, string name, string culture) {
            if (value == null) {
                throw new ArgumentException($"{name}@[{culture}] value cannot be null");
            }
        }

        private void SetVipDescription(string keyLine, string valueLine, string keyCulture, Dictionary<string, KeyFileValues> culture) {
            Dictionary<string, int> levelNameMap = new Dictionary<string, int>() { { "zhizhun", 3 }, { "vip_4", 3 }, { "vip_3", 3 }, { "vip_2", 2 }, { "vip_1", 1 },
            };

            int level = 0;
            foreach (var kv in levelNameMap) {
                if (keyLine.Contains(kv.Key)) {
                    level = kv.Value;
                    break;
                }
            }
            if (level == 0) {
                throw new InvalidDataException($"can not detemine vip level through keyLine: {keyLine}");
            }

            PropertyInfo vipProperty = null;
            foreach (var p in typeof(VipDescription).GetProperties()) {
                var attrs = p.GetCustomAttribute(typeof(KeyFileNamesAttribute)) as KeyFileNamesAttribute;
                if (attrs == null) {
                    continue;
                }
                if (attrs.KeyNames.Any(k => keyLine.Contains(k))) {
                    vipProperty = p;
                    break;
                }
            }

            if (vipProperty == null) {
                log.Warn($"Cannot detemine which vip property from {keyLine}");
                return;
            }

            if (culture.GetOrAddConstruct(keyCulture).VipDescriptions == null) {
                culture.GetOrAddConstruct(keyCulture).VipDescriptions = new Dictionary<int, VipDescription>();
            }
            var desc = culture.GetOrAddConstruct(keyCulture).VipDescriptions.GetOrAddConstruct(level);
            desc.Level = level;
            vipProperty.SetValue(desc, valueLine);
        }

        private List<string> ReadMultiLineValue(string[] lines, ref int index) {
            List<string> values = new List<string>();
            for (; index < lines.Length; ++index) {
                if (string.IsNullOrWhiteSpace(lines[index])) {
                    continue;
                }
                var l = lines[index];
                if (l.StartsWith("-----BEGIN")) {
                    continue;
                }
                if (l.StartsWith("-----END")) {
                    break;
                }
                if (l.Trim().Length == 0) {
                    break;
                }
                values.Add(l.Trim());
            }
            return values;
        }


    }
}
