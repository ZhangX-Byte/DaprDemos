package config

import "github.com/jinzhu/configor"

var Config = struct {
	DB struct {
		Name     string `env:"DBName" default:"golang"`
		Adapter  string `env:"DBAdapter" default:"mysql"`
		Host     string `env:"DBHost" default:"localhost"`
		Port     string `env:"DBPort" default:"3306"`
		User     string `env:"DBUser" default:"root"`
		Password string `env:"DBPassword" default:"123456"`
	}
}{}

func init() {
	if err := configor.Load(&Config, "config/config.yml"); err != nil {
		panic(err)
	}
}
