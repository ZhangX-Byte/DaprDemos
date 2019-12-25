package db

import (
	"daprdemos/golang/customer/config"
	"fmt"

	"github.com/jinzhu/gorm"

	// 初始化mysql
	_ "github.com/jinzhu/gorm/dialects/mysql"
)

// DB Global DB connection
var DB *gorm.DB

func init() {
	var err error

	dbConfig := config.Config.DB
	DB, err = gorm.Open("mysql", fmt.Sprintf("%v:%v@tcp(%v:%v)/%v?parseTime=True&loc=Local", dbConfig.User, dbConfig.Password, dbConfig.Host, dbConfig.Port, dbConfig.Name))

	if err != nil {
		fmt.Println(err)
		panic(err)
	}
}
