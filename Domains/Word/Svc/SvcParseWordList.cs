namespace Ngaq.Local.Word.Svc;

using System.Text;
using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Service.Parser;
using Ngaq.Core.Service.Word;
using Ngaq.Core.Stream;
using Ngaq.Core.Tools.Io;



public  partial class SvcParseWordList
	: ISvcParseWordList
{
	public async Task<nil> AddWordsFromUrlAsy(
		str Path
		, CT ct = default
	) {
		throw new NotImplementedException();
	}

	public async Task<IEnumerable<JnWord>> ParseWordsByIterEtEncodingAsy(
		IIter<u8> Iter
		,Encoding Encoding
		,CT Ct = default
	){
		//I_Answer<IEnumerable<Bo_Word>> ans = new Answer<IEnumerable<Bo_Word>>();
		var Parser = new WordListParser(Iter);
		Parser.Encoding = Encoding;
		var DateBlocks = Parser.Parse();
		var metadata = Parser.Status.Metadata;
		if (metadata == null) {
			throw new ErrBase("Metadata is null");
		}
		var JnWords = ParseResultMapper.Inst.Map(metadata, DateBlocks);
		return JnWords;
	}


	public async Task<IEnumerable<JnWord>> ParseWordsFromFilePath(
		Path_Encode Path_Encode
		, CT ct = default
	) {
		IIter<u8> ByteReader = new ByteReader(Path_Encode.Path);
		return await ParseWordsByIterEtEncodingAsy(ByteReader, Path_Encode.Encoding);
	}

	public async Task<IEnumerable<JnWord>> ParseWordsFromUrlAsy(
		string Path
		, CT ct = default
	) {
		throw new NotImplementedException();
	}

	public async Task<IEnumerable<JnWord>> ParseWordsFromText(
		string Text
		, CT ct = default
	) {
		var ByteReader = new StrByteReader(Text);
		return await ParseWordsByIterEtEncodingAsy(ByteReader, Encoding.UTF8);
	}

}
