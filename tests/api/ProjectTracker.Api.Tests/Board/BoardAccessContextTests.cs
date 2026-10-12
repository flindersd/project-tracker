using ProjectTracker.Api.Modules.Board.Access;
using Xunit;

namespace ProjectTracker.Api.Tests.Board;

public class BoardAccessContextTests
{
    // Given a BoardAccessContext resolved with role Member,
    // When RequireAdministrator() is called,
    // Then a BoardForbiddenException is thrown.
    [Fact]
    public async Task RequireAdministrator_throws_when_role_is_member()
    {
        var stub = new StubBoardAuthorizationService(BoardRole.Member);
        var context = await stub.ResolveAsync(principal: null!, boardId: Guid.NewGuid());

        Assert.Throws<BoardForbiddenException>(() => context.RequireAdministrator());
    }

    // Given a BoardAccessContext resolved with role Administrator,
    // When RequireAdministrator() is called,
    // Then no exception is thrown.
    [Fact]
    public async Task RequireAdministrator_does_not_throw_when_role_is_administrator()
    {
        var stub = new StubBoardAuthorizationService(BoardRole.Administrator);
        var context = await stub.ResolveAsync(principal: null!, boardId: Guid.NewGuid());

        var exception = Record.Exception(() => context.RequireAdministrator());

        Assert.Null(exception);
    }

    // Given BoardAccessContext has no public constructor,
    // When reflection inspects its constructors,
    // Then none of them are public — only module-internal code can mint a context (TD-D).
    [Fact]
    public void BoardAccessContext_has_no_public_constructor()
    {
        var constructors = typeof(BoardAccessContext).GetConstructors(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        Assert.Empty(constructors);
    }

    // Given the stub authorization service,
    // When it resolves a context outside the Development environment without the explicit
    // allow-stub flag,
    // Then it must fail fast rather than silently granting access (R-2).
    [Fact]
    public void StubBoardAuthorizationService_throws_at_construction_outside_development_without_flag()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new StubBoardAuthorizationService(
                BoardRole.Administrator,
                environmentName: "Production",
                allowStubAuthorization: false));
    }
}
