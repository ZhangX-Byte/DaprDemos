package db

import "daprdemos/golang/customer/models"

func init() {
	AutoMigrate(&models.Customer{})
}

// AutoMigrate run auto migration
func AutoMigrate(values ...interface{}) {
	for _, value := range values {
		DB.AutoMigrate(value)
	}
}
