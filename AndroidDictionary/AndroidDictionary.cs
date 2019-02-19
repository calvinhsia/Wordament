using System;
using System.IO;

namespace AndroidDictionary
{
    public class AndroidDictionary
    {
        public enum DictionaryType
        {
            /// <summary>
            /// About 171201 words
            /// </summary>
            Large = 1,
            /// <summary>
            /// 53869 words
            /// </summary>
            Small = 2
        }
        public static string GetResourceInfo()
        {
            var result = string.Empty;
            try
            {

                //Android.Content.Res.Resources.GetObject<byte[]>();
//                var xx = new Android.Content.Res.Resources();
                
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                var names = asm.GetManifestResourceNames(); // "Dictionary.Properties.Resources.resources"
                                                            // AndroidDictionary.Resources.dictdata.dict1.bin

                var strm = asm.GetManifestResourceStream("AndroidDictionary.Resources.dictdata.dict1.bin");
                var dict1 = GetArray(strm);
                //var resman = new System.Resources.ResourceManager("AndroidDictionary.Resources", typeof(AndroidDictionary).Assembly);
                //var dict1 = (byte[])resman.GetObject("dict1.bin", System.Globalization.CultureInfo.CurrentCulture);
                ////var dict1 = (byte[])resman.GetObject("PhoneWord.dict1.bin",culture: System.Globalization.CultureInfo.CurrentCulture);
                result = $"Got {dict1.Length}";
                //txtSrc.Text = $"Got dict 1 {dict1.Length}";
            }
            catch (Exception ex)
            {
                result = ex.ToString();
            }

            return $"Rsrc  {result}";
        }
        public static byte[] GetArray(Stream strm)
        {
            using (var mstrm = new MemoryStream())
            {
                strm.CopyTo(mstrm);
                return mstrm.ToArray();
            }
        }
    }
}
