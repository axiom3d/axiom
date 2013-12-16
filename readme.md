# Axiom 3D Rendering Engine

**Welcome!** 
The Axiom 3D Rendering Engine is a fully object oriented 3D graphics engine using C# and the .Net platform. Axiom 3D aims to be an easy to use, flexible, extendable, and powerful engine that allows for rapid development of games and other graphical applications. By using the .Net framework as the target platform, developers can focus more on core functionality and logic, rather than dealing with the complexities of languages like C++.

# Source Control Ideology

To keep things simple and manageable, we've adopted the [git flow branching model](http://nvie.com/posts/a-successful-git-branching-model) using hgflow in our projects. The creators of git flow have released a [short introduction video](http://vimeo.com/16018419) to explain their model.

## default

The *default* branch of the axiom repository will always contain our latest production (release) code. It should be the most stable source code you can download from us, but also the oldest. New code only gets into *default* when we release a new version or create a hotfix.

## develop

All of our unreleased development work ends up in the *develop* branch. Sometimes it is committed directly, other times it comes from merged hotfixes against a release, and other times it comes from a merged feature branch. This branch will always contain the most bleeding edge axiom code, so it sometimes has bugs and unfinished features. Use this at your own risk, and avoid deploying it in production.

## release

When we're getting ready to tag a release as a beta, we'll branch *develop* into *release*. This allows us to feature-freeze the code and more easily commit bug fixes without having to tediously create hotfix branches for every little thing. This code should be of beta or rc quality, for the most part, and is what you should download if you'd like to help us test.

## feature/x

Feature branches are work-in-progress branches that contain large chunks of new or modified code for a single feature or refactoring task. They are branched to preserve the stability of the *develop* branch during fairly destructive code changes.

## hotfix/x

Hotfixes are branched from *default* and exist to fix small bugs that are detected in a release after it has been tagged in *default*. These branches are usually small and concise, and are merged back into *default* and *develop* once they are completed. They should never be new features.

