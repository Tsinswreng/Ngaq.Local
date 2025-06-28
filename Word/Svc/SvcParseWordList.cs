using System.Text;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Service.Parser;
using Ngaq.Core.Service.Word;
using Ngaq.Core.Stream;
using Ngaq.Core.Tools.Io;
using Ngaq.Core.Word.Models;

namespace Ngaq.Local.Service.Word;

public class SvcParseWordList
	: ISvcParseWordList
{
	public async Task<nil> AddWordsFromUrlAsy(
		str Path
		,CancellationToken ct = default
	) {
		throw new NotImplementedException();
	}

	public async Task<IEnumerable<JnWord>> ParseWordsByIterEtEncodingAsy(
		I_Iter<u8> Iter
		,Encoding Encoding
		,CancellationToken ct = default
	){
		//I_Answer<IEnumerable<Bo_Word>> ans = new Answer<IEnumerable<Bo_Word>>();
		var Parser = new WordListParser(Iter);
		Parser.Encoding = Encoding;
		var DateBlocks = Parser.Parse();
		var metadata = Parser.Status.Metadata;
		if (metadata == null) {
			throw new ErrBase("Metadata is null");
		}
		var Bo_Words = ParseResultMapper.Inst.Map(metadata, DateBlocks);
		return Bo_Words;
	}


	public async Task<IEnumerable<JnWord>> ParseWordsFromFilePath(
		Path_Encode Path_Encode
		,CancellationToken ct = default
	) {
		I_Iter<u8> ByteReader = new ByteReader(Path_Encode.Path);
		return await ParseWordsByIterEtEncodingAsy(ByteReader, Path_Encode.Encoding);
	}

	public async Task<IEnumerable<JnWord>> ParseWordsFromUrlAsy(
		string Path
		,CancellationToken ct = default
	) {
		throw new NotImplementedException();
	}

	public async Task<IEnumerable<JnWord>> ParseWordsFromText(
		string Text
		,CancellationToken ct = default
	) {
		var ByteReader = new StrByteReader(Text);
		return await ParseWordsByIterEtEncodingAsy(ByteReader, Encoding.UTF8);
	}

}
