using System.Collections.Immutable;
using System.Numerics;

namespace CSharpWolfenstein.Tests.Game;

public class EnemyGameObjectTests
{
    protected EnemyGameObject BasicGuard =>
        new EnemyGameObject(
            new BasicGameObjectProperties(
                Vector2D.Zero,
                Vector2D.One,
                0.0f,
                0,
                true,
                true,
                0,
                0,
                0,
                100,
                true
            ),
            new EnemyProperties(
                EnemyType.Guard,
                MapDirection.North,
                ImmutableArray.Create(new[] {0, 1, 2}),
                ImmutableArray.Create(new[] {0, 1, 2}),
                1,
                8,
                0,
                200.0f,
                EnemyState.Standing,
                false,
                false,
                false,
                100,
                0,
                100.0f,
                200.0f
            )
        );
    
    [Fact]
    public void EnemyHasCorrectAnimationTimeForStanding()
    {
        var enemy = BasicGuard;
        var standingEnemy = enemy with {EnemyProperties = enemy.EnemyProperties with { State = EnemyState.Standing}};
        Assert.Equal(0.0f, standingEnemy.AnimationTime);
    }
    
    [Fact]
    public void EnemyHasCorrectAnimationTimeForChase()
    {
        var enemy = BasicGuard;
        var chasingEnemy = enemy with {EnemyProperties = enemy.EnemyProperties with { State = EnemyState.Chase((0,0)) } };
        Assert.Equal(100.0f, chasingEnemy.AnimationTime);
    }
}