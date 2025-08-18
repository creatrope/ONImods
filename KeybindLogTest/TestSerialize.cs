using KSerialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestSerialize
{
    internal class TestSerialize
    {
        [SerializationConfig(MemberSerialization.OptIn)]
        public class TestData : KMonoBehaviour
        {
            [Serialize]
            public HashSet<int> testHashSet = new HashSet<int>();

            public void LoadTestData()
            {
                testHashSet.Clear();
                for (int i = 0; i < 10; i++)
                    testHashSet.Add(i);
            }
            public void PrintTestData()
            {
                string contents = testHashSet.Count > 0
                    ? string.Join(", ", testHashSet)
                    : "(empty)";
                UnityEngine.Debug.Log($"[TestData] testHashSet: {contents}");
            }

            public void ClearTestData()
            {
                testHashSet.Clear();
            }
        }
    }
}
