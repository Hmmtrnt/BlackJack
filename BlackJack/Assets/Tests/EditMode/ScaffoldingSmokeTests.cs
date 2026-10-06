using CsCheck;
using NUnit.Framework;

namespace BJ.Domain.Tests
{
    /// <summary>
    /// タスク1（アセンブリ定義とプロジェクト基盤）のための基盤スモークテストです。
    /// EditMode テストアセンブリが次のことを満たせるか検証します:
    ///   1. UnityEngine 非依存の BJ.Domain アセンブリに対してコンパイルできること
    ///   2. CsCheck（プロパティベーステストライブラリ）を参照・実行できること
    ///
    /// これらは仕様の正しさプロパティ（Property 1〜31）ではありません。それらは
    /// 実際の Domain ロジックに対して個別の `*` サブタスクで実装されます。具体的な
    /// Domain テストが用意できたら、このファイルは削除または置き換えてください。
    /// </summary>
    public sealed class ScaffoldingSmokeTests
    {
        [Test]
        public void Domain_assembly_is_referenceable()
        {
            // BJ.Domain.AssemblyMarker は純粋な C# の Domain アセンブリに存在します。
            var marker = typeof(AssemblyMarker);
            Assert.That(marker.Assembly.GetName().Name, Is.EqualTo("BJ.Domain"));
        }

        [Test]
        public void CsCheck_property_runs_with_100_iterations()
        {
            // CsCheck の DLL がテストアセンブリに正しく組み込まれ、プロパティを
            // 実行できることを確認します。整数の加算は交換法則を満たします。
            Gen.Int.Select(Gen.Int)
                .Sample((a, b) => a + b == b + a, iter: 100);
        }
    }
}
