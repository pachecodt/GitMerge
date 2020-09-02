using System;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace GitMerge
{
    internal class Program
    {
        private static string GetPassword()
        {
            var MyIni = new IniFile("Settings.ini");
            if (MyIni.KeyExists("Password", "Credentials"))
            {
                return MyIni.Read("Password", "Credentials");
            }
            return "";
        }

        private static string GetUser()
        {
            var MyIni = new IniFile("Settings.ini");
            if (MyIni.KeyExists("User", "Credentials"))
            {
                return MyIni.Read("User", "Credentials");
            }
            return "";
        }

        private static void GitMerge(string repository, Branch masterBranch)
        {
            using (var repo = new Repository(repository.ToString()))
            {
                var signature = new Signature(
                   new Identity(GetUser(), "MERGE_USER_EMAIL"), DateTimeOffset.Now);

                MergeOptions opts = new MergeOptions()
                { FileConflictStrategy = CheckoutFileConflictStrategy.Merge };

                Console.WriteLine($"Merge Changes from: { masterBranch.FriendlyName} to { repo.Head}");

                repo.Merge(masterBranch, signature, opts);
            }
        }

        private static void GitPull(string repository)
        {
            string user = GetUser();
            string password = GetPassword();

            if (string.IsNullOrWhiteSpace(user))
                throw new Exception("User invalid.");

            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("User password invalid.");

            using (var repo = new Repository(repository))
            {
                PullOptions options = new PullOptions
                {
                    FetchOptions = new FetchOptions()
                };
                options.FetchOptions.CredentialsProvider = new CredentialsHandler(
                    (url, usernameFromUrl, types) =>

                new UsernamePasswordCredentials()
                {
                    Username = user,
                    Password = password
                });

                var signature = new Signature(new Identity(GetUser() + "Pull", "PULL_EMAIL"), DateTimeOffset.Now);

                try
                {
                    Commands.Pull(repo, signature, options);
                    Console.WriteLine($"Pull Changes in : { repo.Head} ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Pull Changes failed in : { repo.Head} .  { ex.Message}");
                    throw ex;
                }
            }
        }

        private static void Main(string[] args)
        {
            try
            {
                PullBranches();
                Console.WriteLine("Everthing went fine");
            }
            catch (Exception ex)
            {
                Console.WriteLine("we have a problem!");
                throw ex;
            }

            Console.ReadKey();
        }

        private static void PullBranches()
        {
            Console.WriteLine("Repository path:");
            var RepositoryPath = Console.ReadLine();

            using (var repo = new Repository(RepositoryPath.ToString()))
            {
                Branch localBranch = repo.Head;
                Branch masterBranch = repo.Branches["master"];

                if (localBranch == masterBranch)
                {
                    throw new Exception($"Source Brach { localBranch.FriendlyName } is the same as master brach { masterBranch.FriendlyName}.");
                }

                GitPull(RepositoryPath.ToString());

                Commands.Checkout(repo, masterBranch);

                GitPull(RepositoryPath.ToString());

                Commands.Checkout(repo, localBranch);

                var signature = new Signature(
                    new Identity(GetUser(), "MERGE_USER_EMAIL"), DateTimeOffset.Now);

                MergeOptions opts = new MergeOptions()
                { FileConflictStrategy = CheckoutFileConflictStrategy.Merge };

                Console.WriteLine($"Merge Changes from: { masterBranch.FriendlyName} to  { repo.Head}");

                repo.Merge(masterBranch, signature, opts);

                //PushToMaster(RepositoryPath.ToString(), localBranch);
            }
        }

        private static void PushToMaster(string repository, Branch localBranch)
        {
            using (var repo = new Repository(repository))
            {
                PushOptions options = new PushOptions();
                options.CredentialsProvider = new CredentialsHandler(
                    (url, usernameFromUrl, types) =>
                        new UsernamePasswordCredentials()
                        {
                            Username = GetUser(),
                            Password = GetPassword()
                        });
                repo.Network.Push(localBranch, options);
            }
        }
    }
}
