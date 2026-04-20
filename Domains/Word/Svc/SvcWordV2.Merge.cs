namespace Ngaq.Backend.Domains.Word.Svc;

using System.Runtime.CompilerServices;
using Ngaq.Core.Frontend.Kv;
using Ngaq.Core.Infra;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Tools;
using Ngaq.Core.Shared.Sync;
using Ngaq.Core.Shared.StudyPlan.Models.PreFilter;
using Ngaq.Core.Shared.StudyPlan.Models.Po.PreFilter;
using Ngaq.Core.Shared.StudyPlan.Models.Po.StudyPlan;
using Ngaq.Core.Shared.StudyPlan.Svc;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Shared.Word.Models.Learn_;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Core.Shared.Word.Svc;
using Ngaq.Backend.Db.TswG;
using Tsinswreng.CsErr;
using Tsinswreng.CsCore;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;
using Ngaq.Core.Tools.Json;
using Ngaq.Core.Shared.Dictionary.Models;
using Ngaq.Core.Shared.Word.Models.Dto;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Backend.Domains.Word;
using Ngaq.Backend.Domains.Word.Dao;
using Tsinswreng.CsTempus;

public partial class SvcWordV2
{
	public IAsyncEnumerable<IJnWordMergeResult> GetWordMergeResult(IDbUserCtx Ctx, IAsyncEnumerable<JnWord> Words, CT Ct) {
		throw new NotImplementedException();
	}

	public Task<object> MergeWord(IDbUserCtx Ctx, IAsyncEnumerable<IJnWordMergeResult> Words, CT Ct) {
		throw new NotImplementedException();
	}

	public Task<object> MergeWord(IDbUserCtx Ctx, IAsyncEnumerable<JnWord> Words, CT Ct) {
		throw new NotImplementedException();
	}

	public Task<object> MergeWord_NewDescrAsAdd(IDbUserCtx Ctx, IAsyncEnumerable<JnWord> Words, CT Ct) {
		throw new NotImplementedException();
	}
}