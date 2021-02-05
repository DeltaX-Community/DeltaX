namespace DeltaX.Domain.Common.MediatorBehaviors
{
    using DeltaX.Domain.Common.Events;
    using DeltaX.Domain.Common.Extensions;
    using DeltaX.Domain.Common.Repositories; 
    using MediatR;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;

    public class TransactionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ITransactionalRequest
    {
        private readonly ILogger<TransactionBehaviour<TRequest, TResponse>> logger;
        private readonly IUnitOfWork unitOfWork;
        private Func<CancellationToken, Task> excecuteOnFinish; 
        private bool hasActiveTransaction = false; 


        public TransactionBehaviour(IUnitOfWork unitOfWork,
            ILogger<TransactionBehaviour<TRequest, TResponse>> logger, 
            Func<CancellationToken, Task> excecuteOnFinish = null
            )
        {
            this.unitOfWork = unitOfWork ?? throw new ArgumentException(nameof(unitOfWork));
            this.logger = logger ?? throw new ArgumentException(nameof(ILogger)); 
            this.excecuteOnFinish = excecuteOnFinish;
            hasActiveTransaction = false;            
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        { 
            var response = default(TResponse);
            var typeName = request.GetGenericTypeName();
            TransactionScope transaction = null;

            try
            {
                if (hasActiveTransaction)
                {
                    return await next();
                }

                try
                {
                    transaction = new TransactionScope(); 
                    unitOfWork.BeginTransaction();
                    hasActiveTransaction = true;

                    logger.LogInformation("----- Begin transaction for {CommandName} ({@Command})", typeName, request);

                    response = await next();

                    logger.LogInformation("----- Commit transaction for {CommandName}", typeName);

                    unitOfWork.CommitTransaction();
                    transaction?.Complete();
                }
                catch
                {
                    unitOfWork.RollbackTransaction(); 
                    throw;
                }
                finally
                {
                    transaction?.Dispose(); 
                    hasActiveTransaction = false;
                }

                await excecuteOnFinish?.Invoke(cancellationToken);

                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ERROR Handling transaction for {CommandName} ({@Command})", typeName, request);

                throw;
            }
        }
    }
}
