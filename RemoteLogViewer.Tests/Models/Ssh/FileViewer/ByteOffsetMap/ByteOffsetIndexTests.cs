using RemoteLogViewer.Models.Ssh.FileViewer.ByteOffsetMap;
using Shouldly;

namespace RemoteLogViewer.Tests.Models.Ssh.FileViewer.ByteOffsetMap;

public class ByteOffsetIndexTests {
	[Fact]
	public void NewIndex_ShouldHaveCountZero_AndFindReturnsDefault() {
		var index = new ByteOffsetIndex();

		index.Count.ShouldBe(0);
		index.Find(0).ShouldBe(new (0,0));
		index.Find(10).ShouldBe(new (0,0));
	}

	[Fact]
	public void Add_ShouldIncreaseCount_AndAffectFind() {
		var index = new ByteOffsetIndex();
		index.Add(new (5,50));
		index.Add(new (10,120));

		index.Count.ShouldBe(2);

		// targetLine が最初のエントリと等しい場合は、条件が "< targetLine"なので既定値が返る
		index.Find(5).ShouldBe(new (0,0));
		// targetLine が5 と10 の間なら行5 のエントリが返る
		index.Find(7).ShouldBe(new (5,50));
		// targetLine が10 と等しい場合も行5 のエントリが返る
		index.Find(10).ShouldBe(new (5,50));
		// targetLine が最後の行番号より大きければ最後のエントリが返る
		index.Find(100).ShouldBe(new (10,120));
	}

	[Fact]
	public void AddRange_ShouldAppendEntriesInOrder() {
		var index = new ByteOffsetIndex();
		index.Add(new (2,20));
		index.AddRange([new (5, 55), new (9, 99)]);

		index.Count.ShouldBe(3);

		index.Find(1).ShouldBe(new (0,0));
		index.Find(3).ShouldBe(new (2,20));
		index.Find(6).ShouldBe(new (5,55));
		index.Find(10).ShouldBe(new (9,99));
	}

	[Fact]
	public void Reset_ShouldClearAllEntries() {
		var index = new ByteOffsetIndex();
		index.Add(new (1,10));
		index.Add(new (2,30));
		index.Count.ShouldBe(2);

		index.Reset();
		index.Count.ShouldBe(0);
		index.Find(100).ShouldBe(new (0,0));
	}

	[Fact]
	public void Find_WithUnsortedAdds_ShouldReflectInsertionOrderIteration() {
		// 実装は昇順挿入を前提としているため、無秩序な挿入では期待しない結果になる可能性がある。
		var index = new ByteOffsetIndex();
		index.Add(new (100,1000));
		index.Add(new (10,100)); // 大きい行番号の後に小さい行番号を追加

		//走査は最初に "< targetLine" を満たさなくなったところで打ち切るため、targetLine=50 は100 に到達して既定値を返す
		index.Find(50).ShouldBe(new (0,0));
		// targetLine=150 の場合は両方 (100 <150,10 <150) を走査し、最後に見た10 のエントリが結果になる
		//これにより順序を崩した挿入で単調性が崩れることを示す
		index.Find(150).ShouldBe(new (10,100));
	}
}
