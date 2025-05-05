using System.Text;
using Ngaq.Core.Model.Bo;
using Ngaq.Core.Service.Parser;
using Ngaq.Core.Service.Word;
using Ngaq.Core.Stream;
using Ngaq.Core.Util.Io;

namespace Ngaq.Local.Service.Word;

public class Svc_ParseWordList
	: I_Svc_ParseWordList
{
	public async Task<I_Answer<nil>> AddWordsFromUrlAsy(str Path) {
		var ans = new Answer<nil>();
		ans.AddErrStr("Not implemented yet");
		return ans;
	}

	public async Task<I_Answer<IList<Bo_Word>>> ParseWordsByIterEtEncodingAsy(
		I_Iter<u8> Iter
		,Encoding Encoding
	){
		I_Answer<IList<Bo_Word>> ans = new Answer<IList<Bo_Word>>();
		var Parser = new WordListParser(Iter);
		Parser.Encoding = Encoding;
		var DateBlocks = Parser.Parse();
		var metadata = Parser.Status.Metadata;
		if (metadata == null) {
			ans.AddErrStr("Metadata is null");
			return ans;
		}
		var Bo_Words = ParseResultMapper.Inst.Map(metadata, DateBlocks);
		ans.Data = Bo_Words.Data;
		ans.Ok = true;
		return ans;
	}


	public async Task<I_Answer<IList<Bo_Word>>> ParseWordsFromFilePathAsy(Path_Encode Path_Encode) {
		I_Iter<u8> ByteReader = new ByteReader(Path_Encode.Path);
		return await ParseWordsByIterEtEncodingAsy(ByteReader, Path_Encode.Encoding);
	}

	public async Task<I_Answer<IList<Bo_Word>>> ParseWordsFromUrlAsy(string Path) {
		throw new NotImplementedException();
	}

	public async Task<I_Answer<IList<Bo_Word>>> ParseWordsFromTextAsy(string Text) {
		var ByteReader = new StrByteReader(Text);
		return await ParseWordsByIterEtEncodingAsy(ByteReader, Encoding.UTF8);
	}
}
